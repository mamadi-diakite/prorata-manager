namespace ProrataManager.Api.IntegrationTests;

public sealed class SitesEndpointsTests(ProrataManagerApiFactory factory) : IClassFixture<ProrataManagerApiFactory>
{
    private static readonly SiteRequest NewSite = new(
        "Linden Court housing", "12 Avenue des Tilleuls, Lyon", "Cedar Homes", "North Arc Architects",
        new DateOnly(2027, 1, 4), new DateOnly(2028, 6, 30));

    [Fact]
    public async Task Admin_creates_a_site_with_lots_and_contractors()
    {
        var client = await factory.SignedUpAsync("sites.flow@example.com");

        var created = await client.PostAsJsonAsync("/api/sites", NewSite);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var site = (await created.Content.ReadFromJsonAsync<SiteResponse>())!;
        Assert.Equal($"/api/sites/{site.Id}", created.Headers.Location?.AbsolutePath);
        Assert.Equal("Preparation", site.Status);

        var withLots = await SendAsync(client.PostAsJsonAsync(
            $"/api/sites/{site.Id}/lots", new LotRequest("01", "Earthworks and structure", "Structure", 1_845_000.00m)));
        withLots = await SendAsync(client.PostAsJsonAsync(
            $"/api/sites/{site.Id}/lots", new LotRequest("05", "Partitions", "FinishingWorks", null)));
        var structure = withLots.Lots.Single(l => l.Number == "01");
        var partitions = withLots.Lots.Single(l => l.Number == "05");

        var withContractor = await SendAsync(client.PostAsJsonAsync($"/api/sites/{site.Id}/contractors", new ContractorRequest(
            "Atlas Structures", null, null, "Claire Martin", "claire.martin@example.com", null, [structure.Id])));
        var contractor = Assert.Single(withContractor.Contractors);
        Assert.Equal(1_845_000.00m, contractor.ContractAmount);

        await SendAsync(client.PutAsJsonAsync(
            $"/api/sites/{site.Id}/lots/{partitions.Id}", new LotRequest("05", "Partitions and ceilings", "FinishingWorks", 318_720.50m)));
        await SendAsync(client.PutAsJsonAsync($"/api/sites/{site.Id}/contractors/{contractor.Id}", new ContractorRequest(
            "Atlas Structures", "000 000 000 00012", null, "Claire Martin", null, null, [structure.Id, partitions.Id])));
        await SendAsync(client.PutAsJsonAsync($"/api/sites/{site.Id}", NewSite with { ProjectManager = "Lumen Engineering" }));
        await SendAsync(client.PutAsJsonAsync($"/api/sites/{site.Id}/status", new ChangeSiteStatusRequest("InProgress")));

        var saved = await client.GetFromJsonAsync<SiteResponse>($"/api/sites/{site.Id}");
        Assert.Equal(("Lumen Engineering", "InProgress"), (saved!.ProjectManager, saved.Status));
        Assert.Equal(new ContractTotalsResponse(2_163_720.50m, 1_845_000.00m, 318_720.50m, 0m, 0, 0), saved.Totals);
        Assert.Equal(2_163_720.50m, saved.Contractors.Single().ContractAmount);
        Assert.All(saved.Lots, l => Assert.Equal("Atlas Structures", l.ContractorName));

        var list = await client.GetFromJsonAsync<SiteSummaryResponse[]>("/api/sites");
        var summary = Assert.Single(list!);
        Assert.Equal((site.Id, 2, 1, 2_163_720.50m), (summary.Id, summary.LotCount, summary.ContractorCount, summary.TotalContractAmount));
    }

    [Fact]
    public async Task Invalid_requests_return_validation_problems()
    {
        var client = await factory.SignedUpAsync("sites.invalid@example.com");
        var site = await SendAsync(client.PostAsJsonAsync("/api/sites", NewSite));

        var create = await client.PostAsJsonAsync("/api/sites", NewSite with { Name = " ", StartDate = null });
        var lot = await client.PostAsJsonAsync($"/api/sites/{site.Id}/lots", new LotRequest("01", "Structure", "Roofing", 1.001m));

        Assert.Equal(["name", "startDate"], (await ProblemAsync(create)).Errors.Keys.Order());
        Assert.Equal(["contractAmount", "family"], (await ProblemAsync(lot)).Errors.Keys.Order());
    }

    [Fact]
    public async Task Sites_of_another_organization_are_not_found()
    {
        var owner = await factory.SignedUpAsync("sites.owner@example.com");
        var site = await SendAsync(owner.PostAsJsonAsync("/api/sites", NewSite));
        var stranger = await factory.SignedUpAsync("sites.stranger@example.com");

        Assert.Empty((await stranger.GetFromJsonAsync<SiteSummaryResponse[]>("/api/sites"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/sites/{site.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PutAsJsonAsync($"/api/sites/{site.Id}", NewSite)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PutAsJsonAsync(
            $"/api/sites/{site.Id}/lots/{Guid.NewGuid()}", new LotRequest("01", "Structure", "Structure", null))).StatusCode);
    }

    [Fact]
    public async Task Staff_members_work_on_sites_but_cannot_create_them()
    {
        var admin = await factory.SignedUpAsync("sites.admin@example.com");
        var site = await SendAsync(admin.PostAsJsonAsync("/api/sites", NewSite));
        var organization = await admin.GetFromJsonAsync<OrganizationResponse>("/api/organization");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProrataManagerDbContext>();
            var hash = (await db.Users.SingleAsync(u => u.Email == "sites.admin@example.com")).PasswordHash;
            db.Users.Add(new User(organization!.Id, "sites.staff@example.com", "Staff", hash, UserRole.ManagerStaff, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        var staff = await factory.LoggedInAsync("sites.staff@example.com");

        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync("/api/sites", NewSite)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync($"/api/sites/{site.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync(
            $"/api/sites/{site.Id}/lots", new LotRequest("01", "Structure", "Structure", null))).StatusCode);
    }

    [Fact]
    public async Task Needs_a_token_of_an_existing_user()
    {
        var anonymous = factory.CreateClient();
        var withoutSubject = factory.WithToken(factory.TokenWithoutSubject());
        var deleted = await factory.SignedUpAsync("sites.deleted@example.com");
        var site = await SendAsync(deleted.PostAsJsonAsync("/api/sites", NewSite));
        await factory.DeleteUserAsync("sites.deleted@example.com");

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/sites")).StatusCode);
        foreach (var client in new[] { withoutSubject, deleted })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/sites")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/sites", NewSite)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/sites/{site.Id}")).StatusCode);
        }
    }

    private static async Task<SiteResponse> SendAsync(Task<HttpResponseMessage> request)
    {
        var response = await request;
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<SiteResponse>())!;
    }

    private static async Task<ValidationProblemDetails> ProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
    }
}

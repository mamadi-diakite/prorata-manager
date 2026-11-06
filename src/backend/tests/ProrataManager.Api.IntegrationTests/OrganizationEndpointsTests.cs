namespace ProrataManager.Api.IntegrationTests;

public sealed class OrganizationEndpointsTests(ProrataManagerApiFactory factory) : IClassFixture<ProrataManagerApiFactory>
{
    [Fact]
    public async Task Admin_edits_the_details_and_says_the_organization_is_a_third_party()
    {
        var client = await factory.SignedUpAsync("admin.edit@example.com", kind: "Contractor");

        var response = await client.PutAsJsonAsync("/api/organization", new UpdateOrganizationRequest(
            "Prism Site Services", "ThirdParty", "000 000 000 00012", "8 Rue de l'Exemple, Lyon", "+33 4 00 00 00 00"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await client.GetFromJsonAsync<OrganizationResponse>("/api/organization");
        Assert.Equal("Prism Site Services", saved?.Name);
        Assert.Equal("ThirdParty", saved?.Kind);
        Assert.Equal("000 000 000 00012", saved?.RegistrationNumber);
        Assert.Equal("8 Rue de l'Exemple, Lyon", saved?.Address);
        Assert.Equal("+33 4 00 00 00 00", saved?.Phone);
    }

    [Fact]
    public async Task Invalid_details_return_a_validation_problem()
    {
        var client = await factory.SignedUpAsync("admin.invalid@example.com");

        var response = await client.PutAsJsonAsync("/api/organization", new UpdateOrganizationRequest(" ", "Supplier", null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["kind", "name"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Staff_members_can_read_but_not_edit_the_organization()
    {
        var admin = await factory.SignedUpAsync("admin.staff@example.com");
        var organization = await admin.GetFromJsonAsync<OrganizationResponse>("/api/organization");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProrataManagerDbContext>();
            var hash = (await db.Users.SingleAsync(u => u.Email == "admin.staff@example.com")).PasswordHash;
            db.Users.Add(new User(organization!.Id, "staff@example.com", "Staff", hash, UserRole.ManagerStaff, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        var staff = await factory.LoggedInAsync("staff@example.com");

        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/organization")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PutAsJsonAsync(
            "/api/organization", new UpdateOrganizationRequest("Renamed", "ThirdParty", null, null, null))).StatusCode);
    }

    [Fact]
    public async Task Needs_a_token_of_an_existing_user()
    {
        var update = new UpdateOrganizationRequest("Renamed", "ThirdParty", null, null, null);
        var anonymous = factory.CreateClient();
        var withoutSubject = factory.WithToken(factory.TokenWithoutSubject());
        var deleted = await factory.SignedUpAsync("admin.deleted@example.com");
        await factory.DeleteUserAsync("admin.deleted@example.com");

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/organization")).StatusCode);
        foreach (var client in new[] { withoutSubject, deleted })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/organization")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/organization", update)).StatusCode);
        }
    }
}

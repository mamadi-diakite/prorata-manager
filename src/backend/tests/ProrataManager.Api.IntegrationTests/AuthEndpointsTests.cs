namespace ProrataManager.Api.IntegrationTests;

public sealed class AuthEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    private const string DemoPassword = ApiClients.DemoPassword;

    [Fact]
    public async Task Sign_up_creates_the_organization_and_its_admin_and_returns_a_session()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/sign-up", new SignUpRequest(
            "Granite Works", "Contractor", "Jade Roux", "jade.roux@example.com", "NewPass456", "NewPass456"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/auth/me", response.Headers.Location?.AbsolutePath);
        var session = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("jade.roux@example.com", session?.User.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.Equal("Jade Roux", me?.DisplayName);
        Assert.Equal("ManagerAdmin", me?.Role);
        var organization = await client.GetFromJsonAsync<OrganizationResponse>("/api/organization");
        Assert.Equal(me?.OrganizationId, organization?.Id);
        Assert.Equal("Granite Works", organization?.Name);
        Assert.Equal("Contractor", organization?.Kind);
    }

    [Fact]
    public async Task Sign_up_with_errors_returns_a_validation_problem()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/sign-up", new SignUpRequest("", "Supplier", " ", "claire.martin@example.com", "short", "other"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(
            ["confirmPassword", "displayName", "email", "organizationKind", "organizationName", "password"],
            problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Login_with_a_demo_account_returns_a_token()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new LoginRequest("Claire.Martin@Example.com", DemoPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrEmpty(body?.AccessToken));
        Assert.Equal("claire.martin@example.com", body!.User.Email);
        Assert.Equal("ManagerAdmin", body.User.Role);
    }

    [Theory]
    [InlineData("claire.martin@example.com", "WrongPass123")]
    [InlineData("nobody@example.com", DemoPassword)]
    [InlineData("", "")]
    public async Task Login_with_bad_credentials_returns_401(string email, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Email or password is not correct.", problem?.Title);
    }

    [Fact]
    public async Task Me_returns_the_logged_in_user()
    {
        var client = await factory.LoggedInAsync("hugo.lambert@example.com");

        var user = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");

        Assert.Equal("hugo.lambert@example.com", user?.Email);
        Assert.Equal("Hugo Lambert", user?.DisplayName);
    }

    [Fact]
    public async Task Protected_endpoints_need_a_token()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"))).StatusCode);
    }

    [Fact]
    public async Task A_token_without_a_user_id_is_refused()
    {
        var client = factory.WithToken(factory.TokenWithoutSubject());

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"))).StatusCode);
    }

    [Fact]
    public async Task A_token_of_a_deleted_user_is_refused()
    {
        const string email = "temporary@example.com";
        var client = await factory.SignedUpAsync(email);

        await factory.DeleteUserAsync(email);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"))).StatusCode);
    }

    [Fact]
    public async Task Change_password_with_errors_returns_a_validation_problem()
    {
        var client = await factory.LoggedInAsync("claire.martin@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest("WrongPass789", "short", "other"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["confirmPassword", "currentPassword", "newPassword"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Changed_password_replaces_the_old_one()
    {
        var client = await factory.SignedUpAsync("lea.marchand@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("lea.marchand@example.com", DemoPassword))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("lea.marchand@example.com", "NewPass456"))).StatusCode);
    }
}

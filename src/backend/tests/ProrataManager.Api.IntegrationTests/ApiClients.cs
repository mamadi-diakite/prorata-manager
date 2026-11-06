namespace ProrataManager.Api.IntegrationTests;

internal static class ApiClients
{
    public const string DemoPassword = "TestDemo123!";

    public static async Task<HttpClient> LoggedInAsync(this WebApplicationFactory<Program> factory, string email, string password = DemoPassword)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    public static async Task<HttpClient> SignedUpAsync(this WebApplicationFactory<Program> factory, string email, string kind = "ThirdParty")
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/sign-up", new SignUpRequest("Test Organization", kind, "Test User", email, DemoPassword, DemoPassword));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    /// <summary>Signs a token with the API's key but without a user id.</summary>
    public static string TokenWithoutSubject(this WebApplicationFactory<Program> factory, string role = nameof(UserRole.ManagerAdmin))
    {
        var settings = factory.Services.GetRequiredService<JwtSettings>();

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { [JwtClaims.Email] = "ghost@example.com", [JwtClaims.Role] = role },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
        });
    }

    public static HttpClient WithToken(this WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>Removes the user behind the email, so its token no longer matches anyone.</summary>
    public static async Task DeleteUserAsync(this WebApplicationFactory<Program> factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProrataManagerDbContext>();
        db.Users.Remove(db.Users.Single(u => u.Email == email));
        await db.SaveChangesAsync();
    }
}

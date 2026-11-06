namespace ProrataManager.Api.IntegrationTests;

public sealed class DemoDataStartupTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Fact]
    public async Task Development_startup_seeds_demo_data_when_enabled()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProrataManagerDbContext>();

        Assert.Equal(DemoData.Organizations.Count, await db.Organizations.CountAsync());
        Assert.True(await db.Users.AnyAsync(u => u.Email == "claire.martin@example.com"));
    }
}

public sealed class ProductionDemoDataStartupTests(ProductionDemoDataApiFactory factory)
    : IClassFixture<ProductionDemoDataApiFactory>
{
    [Fact]
    public async Task Startup_outside_development_never_seeds_demo_data()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProrataManagerDbContext>();

        Assert.Equal(0, await db.Users.CountAsync());
    }
}

public sealed class SigningKeyStartupTests(NoSigningKeyApiFactory factory) : IClassFixture<NoSigningKeyApiFactory>
{
    [Fact]
    public void Startup_fails_without_a_signing_key()
    {
        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("Jwt:SigningKey is missing", error.Message);
    }
}

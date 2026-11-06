namespace ProrataManager.Api.IntegrationTests;

/// <summary>Runs the API against its own throwaway SQLite file, with a test-only signing key.</summary>
public class ProrataManagerApiFactory : WebApplicationFactory<Program>
{
    public static readonly string TestSigningKey =
        Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

    private readonly string _root = Path.Combine(Path.GetTempPath(), "prorata-manager-tests", Guid.NewGuid().ToString("N"));

    protected virtual string EnvironmentName => "Development";

    protected virtual bool SeedDemoData => false;

    protected virtual string SigningKey => TestSigningKey;

    protected virtual string ConnectionString => $"Data Source={Path.Combine(_root, "app.db")}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Database:SeedDemoData", SeedDemoData.ToString());
        builder.UseSetting("Jwt:SigningKey", SigningKey);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

public sealed class DemoDataApiFactory : ProrataManagerApiFactory
{
    protected override bool SeedDemoData => true;
}

public sealed class ProductionDemoDataApiFactory : ProrataManagerApiFactory
{
    protected override string EnvironmentName => "Production";

    protected override bool SeedDemoData => true;
}

public sealed class NoSigningKeyApiFactory : ProrataManagerApiFactory
{
    protected override string SigningKey => string.Empty;
}

public sealed class NoConnectionStringApiFactory : ProrataManagerApiFactory
{
    protected override string ConnectionString => string.Empty;
}

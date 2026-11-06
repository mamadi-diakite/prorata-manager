namespace ProrataManager.Infrastructure.Tests;

public sealed class DemoDataSeederTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly IdentityPasswordHasher _hasher = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Seeds_a_contractor_and_a_third_party_manager_with_their_admins()
    {
        Assert.True(await SeedAsync());

        await using var db = _database.CreateContext();
        var organizations = await db.Organizations.ToDictionaryAsync(o => o.Id);
        var users = await db.Users.ToListAsync();

        Assert.Equal([ManagerKind.Contractor, ManagerKind.ThirdParty], organizations.Values.Select(o => o.Kind).Order());
        Assert.Equal(DemoData.Organizations.Select(o => o.AdminEmail).Order(), users.Select(u => u.Email).Order());
        Assert.All(users, u => Assert.Equal(UserRole.ManagerAdmin, u.Role));
        Assert.All(users, u => Assert.Contains(u.OrganizationId, organizations.Keys));
        Assert.All(users, u => Assert.EndsWith("@example.com", u.Email));
        Assert.All(users, u => Assert.Equal(Now, u.CreatedAtUtc));
    }

    [Fact]
    public async Task Every_demo_account_accepts_the_demo_password()
    {
        await SeedAsync();

        await using var db = _database.CreateContext();
        foreach (var user in await db.Users.ToListAsync())
        {
            Assert.True(_hasher.Verify(user.PasswordHash, DemoDataSeeder.DemoPassword), user.Email);
        }
    }

    [Fact]
    public async Task Seeds_sites_with_lots_in_the_three_families_and_their_contractors()
    {
        await SeedAsync();

        await using var db = _database.CreateContext();
        var sites = await db.Sites.ToListAsync();
        var lots = await db.Lots.ToListAsync();
        var contractors = await db.Contractors.ToDictionaryAsync(c => c.Id);

        Assert.Equal(DemoData.Organizations.Sum(o => o.Sites.Count), sites.Count);
        Assert.Equal([SiteStatus.Preparation, SiteStatus.InProgress, SiteStatus.HandedOver], sites.Select(s => s.Status).Order());
        Assert.Equal(Enum.GetValues<LotFamily>(), lots.Select(l => l.Family).Distinct().Order());
        Assert.All(lots.Where(l => l.ContractorId is not null), l => Assert.Equal(l.SiteId, contractors[l.ContractorId!.Value].SiteId));
        Assert.All(contractors.Values, c => Assert.EndsWith("@example.com", c.ContactEmail));

        var linden = sites.Single(s => s.Name == "Linden Court housing");
        var lindenLots = lots.Where(l => l.SiteId == linden.Id).ToList();
        Assert.Equal(3_401_450.95m, lindenLots.Sum(l => l.ContractAmount ?? 0));
        var painting = Assert.Single(lindenLots, l => l.Number == "06");
        Assert.Null(painting.ContractorId);
        Assert.Null(painting.ContractAmount);
    }

    [Fact]
    public async Task Second_run_adds_nothing()
    {
        await SeedAsync();

        Assert.False(await SeedAsync());

        await using var db = _database.CreateContext();
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(2, await db.Organizations.CountAsync());
        Assert.Equal(3, await db.Sites.CountAsync());
    }

    [Fact]
    public async Task Skips_a_database_that_already_has_users()
    {
        var organization = new ManagerOrganization("Someone", ManagerKind.ThirdParty, Now);
        await using (var db = _database.CreateContext())
        {
            db.Organizations.Add(organization);
            db.Users.Add(new User(organization.Id, "someone@example.com", "Someone", "hash", UserRole.ManagerAdmin, Now));
            await db.SaveChangesAsync();
        }

        Assert.False(await SeedAsync());

        await using var check = _database.CreateContext();
        Assert.Equal(1, await check.Users.CountAsync());
        Assert.Equal(0, await check.Sites.CountAsync());
    }

    private async Task<bool> SeedAsync()
    {
        await using var db = _database.CreateContext();

        return await new DemoDataSeeder(db, _hasher, new FixedTimeProvider(Now)).SeedAsync();
    }
}

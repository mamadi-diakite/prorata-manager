namespace ProrataManager.Infrastructure.Tests;

public sealed class ProrataManagerDbContextTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Saves_and_reads_an_organization_with_its_admin()
    {
        var organization = new ManagerOrganization("Prism Site Services", ManagerKind.ThirdParty, Now);
        organization.UpdateDetails("Prism Site Services", ManagerKind.ThirdParty, "000 000 000 00012", "8 Rue de l'Exemple", null);
        var admin = new User(organization.Id, "claire@example.com", "Claire", "hash", UserRole.ManagerAdmin, Now);

        await using (var db = _database.CreateContext())
        {
            db.Organizations.Add(organization);
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var loaded = await db.Organizations.SingleAsync();
            Assert.Equal(ManagerKind.ThirdParty, loaded.Kind);
            Assert.Equal("000 000 000 00012", loaded.RegistrationNumber);
            Assert.Equal(DateTimeKind.Utc, loaded.CreatedAtUtc.Kind);
            var user = await db.Users.SingleAsync();
            Assert.Equal(organization.Id, user.OrganizationId);
            Assert.Equal(UserRole.ManagerAdmin, user.Role);
        }
    }

    [Fact]
    public async Task Saves_and_reads_a_site_with_its_lots_and_contractors()
    {
        var site = await AddSiteAsync();
        var contractor = new Contractor(site.Id, new ContractorDetails("Brightline Electrics", ContactEmail: "hugo@example.com"), Now);
        var lot = new Lot(site.Id, "08", "Electrical works", LotFamily.BuildingServices);
        lot.AssignTo(contractor.Id);
        lot.SetContractAmount(412_380.45m);

        await using (var db = _database.CreateContext())
        {
            db.Contractors.Add(contractor);
            db.Lots.Add(lot);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var loadedSite = await db.Sites.SingleAsync();
            Assert.Equal(new DateOnly(2027, 1, 4), loadedSite.StartDate);
            Assert.Equal(new DateOnly(2028, 6, 30), loadedSite.PlannedHandoverDate);
            Assert.Equal(SiteStatus.Preparation, loadedSite.Status);

            var loadedLot = await db.Lots.SingleAsync();
            Assert.Equal(LotFamily.BuildingServices, loadedLot.Family);
            Assert.Equal(contractor.Id, loadedLot.ContractorId);
            Assert.Equal(412_380.45m, loadedLot.ContractAmount);
            Assert.Equal("hugo@example.com", (await db.Contractors.SingleAsync()).ContactEmail);
        }
    }

    [Fact]
    public async Task Email_is_unique()
    {
        var organization = await AddOrganizationAsync();
        await using var db = _database.CreateContext();
        db.Users.Add(new User(organization.Id, "admin@example.com", "Admin", "hash", UserRole.ManagerAdmin, Now));
        db.Users.Add(new User(organization.Id, "Admin@Example.com", "Admin 2", "hash", UserRole.ManagerAdmin, Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Lot_number_is_unique_within_a_site()
    {
        var site = await AddSiteAsync();
        await using var db = _database.CreateContext();
        db.Lots.Add(new Lot(site.Id, "01", "Earthworks", LotFamily.Structure));
        db.Lots.Add(new Lot(site.Id, "01", "Foundations", LotFamily.Structure));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task A_contractor_holding_a_lot_cannot_be_deleted()
    {
        var site = await AddSiteAsync();
        var contractor = new Contractor(site.Id, new ContractorDetails("Brightline Electrics"), Now);
        var lot = new Lot(site.Id, "08", "Electrical works", LotFamily.BuildingServices);
        lot.AssignTo(contractor.Id);
        await using (var db = _database.CreateContext())
        {
            db.Contractors.Add(contractor);
            db.Lots.Add(lot);
            await db.SaveChangesAsync();
        }

        await using var context = _database.CreateContext();
        context.Contractors.Remove(await context.Contractors.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private async Task<ManagerOrganization> AddOrganizationAsync()
    {
        var organization = new ManagerOrganization("Atlas Structures", ManagerKind.Contractor, Now);
        await using var db = _database.CreateContext();
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

        return organization;
    }

    private async Task<Site> AddSiteAsync()
    {
        var organization = await AddOrganizationAsync();
        var site = new Site(
            organization.Id, "Linden Court housing", "12 Avenue des Tilleuls, Lyon", "Cedar Homes", "North Arc Architects",
            new DateOnly(2027, 1, 4), new DateOnly(2028, 6, 30), Now);
        await using var db = _database.CreateContext();
        db.Sites.Add(site);
        await db.SaveChangesAsync();

        return site;
    }
}

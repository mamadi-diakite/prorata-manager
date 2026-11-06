namespace ProrataManager.Infrastructure.Tests;

public sealed class SiteRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Start = new(2027, 1, 4);
    private static readonly DateOnly Handover = new(2028, 6, 30);

    private readonly SqliteTestDatabase _database = new();
    private readonly ManagerOrganization _organization = new("Atlas Structures", ManagerKind.Contractor, Now);
    private readonly ManagerOrganization _other = new("Prism Site Services", ManagerKind.ThirdParty, Now);
    private readonly Site _linden;
    private readonly Site _harbour;
    private readonly Site _maple;

    public SiteRepositoryTests()
    {
        _linden = NewSite(_organization, "Linden Court housing");
        _harbour = NewSite(_organization, "Harbour Point offices");
        _maple = NewSite(_other, "Maple School extension");
        using var db = _database.CreateContext();
        db.Organizations.AddRange(_organization, _other);
        db.Sites.AddRange(_linden, _harbour, _maple);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Lists_and_finds_only_the_sites_of_the_organization()
    {
        await using var db = _database.CreateContext();
        var repository = new SiteRepository(db);

        Assert.Equal(["Harbour Point offices", "Linden Court housing"], (await repository.ListAsync(_organization.Id, default)).Select(s => s.Name));
        Assert.Equal(_linden.Id, (await repository.FindAsync(_organization.Id, _linden.Id, default))?.Id);
        Assert.Null(await repository.FindAsync(_organization.Id, _maple.Id, default));
        Assert.Null(await repository.FindAsync(_organization.Id, Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Adds_lots_and_contractors_and_lists_them_by_site()
    {
        Guid contractorId;
        await using (var db = _database.CreateContext())
        {
            var repository = new SiteRepository(db);
            var contractor = new Contractor(_linden.Id, new ContractorDetails("Atlas Structures"), Now);
            var lot = new Lot(_linden.Id, "01", "Earthworks and structure", LotFamily.Structure);
            lot.AssignTo(contractor.Id);
            lot.SetContractAmount(1_845_000.00m);
            repository.Add(contractor);
            repository.Add(lot);
            repository.Add(new Lot(_maple.Id, "01", "Extension structure", LotFamily.Structure));
            repository.Add(new Contractor(_maple.Id, new ContractorDetails("Summit Roofing"), Now));
            (await repository.FindAsync(_organization.Id, _harbour.Id, default))!.ChangeStatus(SiteStatus.InProgress);
            await repository.SaveChangesAsync(default);
            contractorId = contractor.Id;
        }

        await using var check = _database.CreateContext();
        var reader = new SiteRepository(check);
        var lots = await reader.ListLotsAsync([_linden.Id, _harbour.Id], default);
        var contractors = await reader.ListContractorsAsync([_linden.Id], default);

        var stored = Assert.Single(lots);
        Assert.Equal((contractorId, 1_845_000.00m), (stored.ContractorId!.Value, stored.ContractAmount!.Value));
        Assert.Equal("Atlas Structures", Assert.Single(contractors).CompanyName);
        Assert.Equal(2, (await reader.ListLotsAsync([_linden.Id, _maple.Id], default)).Count);
        Assert.Empty(await reader.ListContractorsAsync([], default));
        Assert.Equal(SiteStatus.InProgress, (await reader.FindAsync(_organization.Id, _harbour.Id, default))?.Status);
    }

    [Fact]
    public async Task A_site_is_saved_with_its_dates()
    {
        var site = NewSite(_organization, "Cedar Lane depot");
        await using (var db = _database.CreateContext())
        {
            var repository = new SiteRepository(db);
            repository.Add(site);
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        var stored = await new SiteRepository(check).FindAsync(_organization.Id, site.Id, default);
        Assert.Equal((Start, Handover, Now), (stored!.StartDate, stored.PlannedHandoverDate, stored.CreatedAtUtc));
    }

    private static Site NewSite(ManagerOrganization organization, string name) =>
        new(organization.Id, name, "12 Avenue des Tilleuls", "Cedar Homes", "North Arc Architects", Start, Handover, Now);
}

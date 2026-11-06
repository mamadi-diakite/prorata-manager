namespace ProrataManager.Application.Tests;

public sealed class SiteServiceTests
{
    private static readonly DateOnly Start = new(2027, 1, 4);
    private static readonly DateOnly Handover = new(2028, 6, 30);

    private readonly InMemoryUserRepository _users = new();
    private readonly InMemorySiteRepository _sites = new();
    private readonly SiteService _service;
    private readonly User _admin;
    private readonly Site _site;

    public SiteServiceTests()
    {
        _service = new SiteService(_users, _sites, new FixedTimeProvider(TestClock.Now));
        var organization = new ManagerOrganization("Atlas Structures", ManagerKind.Contractor, TestClock.Now.UtcDateTime);
        _admin = new User(organization.Id, "claire@example.com", "Claire", "hash", UserRole.ManagerAdmin, TestClock.Now.UtcDateTime);
        _users.Organizations.Add(organization);
        _users.Users.Add(_admin);
        _site = new Site(organization.Id, "Linden Court housing", "12 Avenue des Tilleuls", "Cedar Homes", "North Arc Architects", Start, Handover, TestClock.Now.UtcDateTime);
        _sites.Sites.Add(_site);
    }

    [Fact]
    public async Task Lists_the_sites_of_the_organization_with_counts_and_totals()
    {
        var contractor = AddContractor("Atlas Structures");
        AddLot("01", LotFamily.Structure, contractor, 1_845_000m);
        AddLot("06", LotFamily.FinishingWorks, null, null);
        var other = new Site(Guid.NewGuid(), "Elsewhere", "Address", "Owner", "Manager", Start, Handover, TestClock.Now.UtcDateTime);
        _sites.Sites.Add(other);
        var empty = new Site(_admin.OrganizationId, "Harbour Point offices", "Quai", "Owner", "Manager", Start, Handover, TestClock.Now.UtcDateTime);
        _sites.Sites.Add(empty);

        var result = await _service.ListAsync(_admin.Id);

        Assert.Equal(SiteOutcome.Ok, result.Outcome);
        Assert.Equal(
            new[]
            {
                new SiteSummaryResponse(empty.Id, "Harbour Point offices", "Quai", "Preparation", Start, Handover, 0, 0, 0m),
                new SiteSummaryResponse(
                    _site.Id, "Linden Court housing", "12 Avenue des Tilleuls", "Preparation", Start, Handover, 2, 1, 1_845_000m),
            },
            result.Value);
    }

    [Fact]
    public async Task Unknown_user_is_refused_everywhere()
    {
        var unknown = Guid.NewGuid();

        Assert.Equal(SiteOutcome.UserNotFound, (await _service.ListAsync(unknown)).Outcome);
        Assert.Equal(SiteOutcome.UserNotFound, (await _service.GetAsync(unknown, _site.Id)).Outcome);
        Assert.Equal(SiteOutcome.UserNotFound, (await _service.CreateAsync(unknown, ValidSite())).Outcome);
        Assert.Equal(SiteOutcome.UserNotFound, (await _service.UpdateAsync(unknown, _site.Id, ValidSite())).Outcome);
        Assert.Equal(0, _sites.SaveCount);
    }

    [Fact]
    public async Task A_site_of_another_organization_is_not_found()
    {
        var other = new Site(Guid.NewGuid(), "Elsewhere", "Address", "Owner", "Manager", Start, Handover, TestClock.Now.UtcDateTime);
        _sites.Sites.Add(other);

        Assert.Equal(SiteOutcome.NotFound, (await _service.GetAsync(_admin.Id, other.Id)).Outcome);
        Assert.Equal(SiteOutcome.NotFound, (await _service.AddLotAsync(_admin.Id, other.Id, ValidLot())).Outcome);
        Assert.Equal(SiteOutcome.NotFound, (await _service.GetAsync(_admin.Id, Guid.NewGuid())).Outcome);
    }

    [Fact]
    public async Task Creates_a_site_in_preparation()
    {
        var result = await _service.CreateAsync(_admin.Id, new SiteRequest(
            " Maple School extension ", " 27 Rue des Erables ", " Westfield Town Council ", " Lumen Engineering ", Start, Handover));

        Assert.Equal(SiteOutcome.Ok, result.Outcome);
        var site = Assert.Single(_sites.Sites, s => s.Name == "Maple School extension");
        Assert.Equal(_admin.OrganizationId, site.OrganizationId);
        Assert.Equal(TestClock.Now.UtcDateTime, site.CreatedAtUtc);
        Assert.Equal(1, _sites.SaveCount);
        var response = result.Value!;
        Assert.Equal(
            (site.Id, "Maple School extension", "27 Rue des Erables", "Westfield Town Council", "Lumen Engineering", Start, Handover, "Preparation"),
            (response.Id, response.Name, response.Address, response.ProjectOwner, response.ProjectManager,
                response.StartDate, response.PlannedHandoverDate, response.Status));
        Assert.Empty(response.Lots);
        Assert.Empty(response.Contractors);
        Assert.Equal(new ContractTotalsResponse(0m, 0m, 0m, 0m, 0, 0), response.Totals);
    }

    [Fact]
    public async Task Create_reports_every_missing_field()
    {
        var result = await _service.CreateAsync(_admin.Id, new SiteRequest(" ", "", " ", "", null, null));

        Assert.Equal(SiteOutcome.Invalid, result.Outcome);
        Assert.Equal(
            ["address", "name", "plannedHandoverDate", "projectManager", "projectOwner", "startDate"],
            result.Errors.Keys.Order());
        Assert.Single(_sites.Sites);
        Assert.Equal(0, _sites.SaveCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public async Task Planned_handover_must_be_after_the_start(int days)
    {
        var result = await _service.CreateAsync(_admin.Id, ValidSite() with { PlannedHandoverDate = Start.AddDays(days) });

        Assert.Equal(["Planned handover must be after the start date."], result.Errors["plannedHandoverDate"]);
    }

    [Fact]
    public async Task Handover_alone_is_not_compared_with_a_missing_start()
    {
        var result = await _service.CreateAsync(_admin.Id, ValidSite() with { StartDate = null });

        Assert.Equal(["startDate"], result.Errors.Keys);
    }

    [Fact]
    public async Task Create_and_update_reject_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(_admin.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(_admin.Id, _site.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ChangeStatusAsync(_admin.Id, _site.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddLotAsync(_admin.Id, _site.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateLotAsync(_admin.Id, _site.Id, Guid.NewGuid(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddContractorAsync(_admin.Id, _site.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.UpdateContractorAsync(_admin.Id, _site.Id, Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task Updates_the_site_details()
    {
        var result = await _service.UpdateAsync(_admin.Id, _site.Id, new SiteRequest(
            "Linden Court", "14 Avenue des Tilleuls", "Cedar Homes", "North Arc", Start.AddDays(7), Handover.AddDays(7)));

        Assert.Equal(SiteOutcome.Ok, result.Outcome);
        Assert.Equal("Linden Court", _site.Name);
        Assert.Equal("North Arc", _site.ProjectManager);
        Assert.Equal(Start.AddDays(7), _site.StartDate);
        Assert.Equal(1, _sites.SaveCount);
    }

    [Fact]
    public async Task Invalid_update_changes_nothing()
    {
        var result = await _service.UpdateAsync(_admin.Id, _site.Id, ValidSite() with { Name = " " });

        Assert.Equal(SiteOutcome.Invalid, result.Outcome);
        Assert.Equal("Linden Court housing", _site.Name);
        Assert.Equal(0, _sites.SaveCount);
    }

    [Fact]
    public async Task Status_moves_forward()
    {
        var result = await _service.ChangeStatusAsync(_admin.Id, _site.Id, new ChangeSiteStatusRequest("inProgress"));

        Assert.Equal("InProgress", result.Value?.Status);
        Assert.Equal(SiteStatus.InProgress, _site.Status);
        Assert.Equal(1, _sites.SaveCount);
    }

    [Theory]
    [InlineData("Preparation", "A site cannot go back to an earlier status.")]
    [InlineData("Finished", "Choose a status.")]
    public async Task Status_cannot_go_back_or_be_unknown(string status, string message)
    {
        _site.ChangeStatus(SiteStatus.InProgress);

        var result = await _service.ChangeStatusAsync(_admin.Id, _site.Id, new ChangeSiteStatusRequest(status));

        Assert.Equal([message], result.Errors["status"]);
        Assert.Equal(SiteStatus.InProgress, _site.Status);
        Assert.Equal(0, _sites.SaveCount);
    }

    [Fact]
    public async Task Adds_a_lot_with_its_family_and_amount()
    {
        var result = await _service.AddLotAsync(_admin.Id, _site.Id, new LotRequest(" 08a ", " Electrical works ", "buildingServices", 412_380.45m));

        var lot = Assert.Single(_sites.Lots);
        Assert.Equal("08A", lot.Number);
        Assert.Equal(LotFamily.BuildingServices, lot.Family);
        Assert.Equal(412_380.45m, lot.ContractAmount);
        Assert.Equal(1, _sites.SaveCount);
        Assert.Equal(new LotResponse(lot.Id, "08A", "Electrical works", "BuildingServices", null, null, 412_380.45m), result.Value?.Lots.Single());
        Assert.Equal(new ContractTotalsResponse(412_380.45m, 0m, 0m, 412_380.45m, 0, 1), result.Value?.Totals);
    }

    [Fact]
    public async Task A_lot_amount_can_wait()
    {
        var result = await _service.AddLotAsync(_admin.Id, _site.Id, ValidLot() with { ContractAmount = null });

        Assert.Null(Assert.Single(_sites.Lots).ContractAmount);
        Assert.Equal(1, result.Value?.Totals.LotsWithoutAmount);
    }

    [Fact]
    public async Task Lot_reports_every_invalid_field()
    {
        var result = await _service.AddLotAsync(_admin.Id, _site.Id, new LotRequest(" ", " ", "Roofing", 10.005m));

        Assert.Equal(["contractAmount", "family", "name", "number"], result.Errors.Keys.Order());
        Assert.Equal([SiteService.FamilyMessage], result.Errors["family"]);
        Assert.Equal([SiteService.AmountMessage], result.Errors["contractAmount"]);
        Assert.Empty(_sites.Lots);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Lot_amount_must_be_positive(string amount)
    {
        var result = await _service.AddLotAsync(
            _admin.Id, _site.Id, ValidLot() with { ContractAmount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture) });

        Assert.Equal(["contractAmount"], result.Errors.Keys);
    }

    [Fact]
    public async Task Lot_number_is_unique_within_the_site()
    {
        AddLot("01", LotFamily.Structure, null, null);

        var result = await _service.AddLotAsync(_admin.Id, _site.Id, ValidLot() with { Number = " 01 " });

        Assert.Equal(["Another lot of this site already uses this number."], result.Errors["number"]);
        Assert.Single(_sites.Lots);
    }

    [Fact]
    public async Task Updates_a_lot_and_keeps_its_own_number()
    {
        var lot = AddLot("01", LotFamily.Structure, null, 100m);

        var result = await _service.UpdateLotAsync(
            _admin.Id, _site.Id, lot.Id, new LotRequest("01", "Earthworks and structure", "Structure", 1_845_000m));

        Assert.Equal(SiteOutcome.Ok, result.Outcome);
        Assert.Equal("Earthworks and structure", lot.Name);
        Assert.Equal(1_845_000m, lot.ContractAmount);
        Assert.Equal(1, _sites.SaveCount);
    }

    [Fact]
    public async Task Invalid_lot_update_changes_nothing()
    {
        var lot = AddLot("01", LotFamily.Structure, null, 100m);
        AddLot("02", LotFamily.Structure, null, null);

        var result = await _service.UpdateLotAsync(_admin.Id, _site.Id, lot.Id, ValidLot() with { Number = "02" });

        Assert.Equal(SiteOutcome.Invalid, result.Outcome);
        Assert.Equal("01", lot.Number);
        Assert.Equal(100m, lot.ContractAmount);
    }

    [Fact]
    public async Task Unknown_lot_or_contractor_is_not_found()
    {
        Assert.Equal(SiteOutcome.NotFound, (await _service.UpdateLotAsync(_admin.Id, _site.Id, Guid.NewGuid(), ValidLot())).Outcome);
        Assert.Equal(
            SiteOutcome.NotFound,
            (await _service.UpdateContractorAsync(_admin.Id, _site.Id, Guid.NewGuid(), ValidContractor())).Outcome);
        Assert.Equal(0, _sites.SaveCount);
    }

    [Fact]
    public async Task Adds_a_contractor_and_gives_it_lots()
    {
        var structure = AddLot("01", LotFamily.Structure, null, 1_845_000m);
        var roofing = AddLot("02", LotFamily.Structure, null, 286_450m);
        AddLot("06", LotFamily.FinishingWorks, null, null);

        var result = await _service.AddContractorAsync(_admin.Id, _site.Id, new ContractorRequest(
            " Atlas Structures ", " 000 000 000 00012 ", " 1 Rue du Chantier ", " Claire Martin ", " Claire.Martin@Example.com ", " 0400 ",
            [roofing.Id, structure.Id, structure.Id]));

        var contractor = Assert.Single(_sites.Contractors);
        Assert.Equal("Atlas Structures", contractor.CompanyName);
        Assert.Equal("claire.martin@example.com", contractor.ContactEmail);
        Assert.Equal(TestClock.Now.UtcDateTime, contractor.CreatedAtUtc);
        Assert.Equal(contractor.Id, structure.ContractorId);
        Assert.Equal(contractor.Id, roofing.ContractorId);
        Assert.Equal(1, _sites.SaveCount);
        var response = result.Value!.Contractors.Single();
        Assert.Equal(
            (contractor.Id, "Atlas Structures", "000 000 000 00012", "1 Rue du Chantier", "Claire Martin", "claire.martin@example.com", "0400"),
            (response.Id, response.CompanyName, response.RegistrationNumber, response.Address, response.ContactName,
                response.ContactEmail, response.Phone));
        Assert.Equal([structure.Id, roofing.Id], response.LotIds);
        Assert.Equal(2_131_450m, response.ContractAmount);
        Assert.Equal(["Atlas Structures", "Atlas Structures", null], result.Value.Lots.Select(l => l.ContractorName));
    }

    [Fact]
    public async Task A_contractor_may_start_without_lots_or_contact()
    {
        var result = await _service.AddContractorAsync(
            _admin.Id, _site.Id, new ContractorRequest("Summit Roofing", null, null, null, " ", null, null));

        Assert.Equal(SiteOutcome.Ok, result.Outcome);
        Assert.Null(Assert.Single(_sites.Contractors).ContactEmail);
        Assert.Equal(0m, result.Value!.Contractors.Single().ContractAmount);
    }

    [Fact]
    public async Task Contractor_reports_every_invalid_field()
    {
        var tooLong = new string('x', 301);

        var result = await _service.AddContractorAsync(
            _admin.Id, _site.Id, new ContractorRequest(" ", tooLong, tooLong, tooLong, "claire@", tooLong, [Guid.NewGuid(), Guid.NewGuid()]));

        Assert.Equal(
            ["address", "companyName", "contactEmail", "contactName", "lotIds", "phone", "registrationNumber"],
            result.Errors.Keys.Order());
        Assert.Equal(["Choose lots of this site."], result.Errors["lotIds"]);
        Assert.Empty(_sites.Contractors);
        Assert.Equal(0, _sites.SaveCount);
    }

    [Fact]
    public async Task A_lot_held_by_another_contractor_cannot_be_given()
    {
        var holder = AddContractor("Clearwall Interiors");
        var lot = AddLot("05", LotFamily.FinishingWorks, holder, 318_720.50m);

        var result = await _service.AddContractorAsync(_admin.Id, _site.Id, ValidContractor() with { LotIds = [lot.Id] });

        Assert.Equal(["Lot 05 is already held by Clearwall Interiors."], result.Errors["lotIds"]);
        Assert.Equal(holder.Id, lot.ContractorId);
        Assert.Single(_sites.Contractors);
    }

    [Fact]
    public async Task Updates_a_contractor_and_moves_its_lots()
    {
        var contractor = AddContractor("Aquaflow");
        var kept = AddLot("09", LotFamily.BuildingServices, contractor, 538_900m);
        var released = AddLot("10", LotFamily.BuildingServices, contractor, 50_000m);
        var added = AddLot("11", LotFamily.BuildingServices, null, 20_000m);
        var others = AddLot("12", LotFamily.BuildingServices, AddContractor("Brightline Electrics"), 1_000m);

        var result = await _service.UpdateContractorAsync(
            _admin.Id, _site.Id, contractor.Id, ValidContractor() with { CompanyName = "Aquaflow Services", LotIds = [kept.Id, added.Id] });

        Assert.Equal(SiteOutcome.Ok, result.Outcome);
        Assert.Equal("Aquaflow Services", contractor.CompanyName);
        Assert.Equal(contractor.Id, kept.ContractorId);
        Assert.Null(released.ContractorId);
        Assert.Equal(contractor.Id, added.ContractorId);
        Assert.NotNull(others.ContractorId);
        Assert.Equal(558_900m, result.Value!.Contractors.Single(c => c.Id == contractor.Id).ContractAmount);
        Assert.Equal(1, _sites.SaveCount);
    }

    [Fact]
    public async Task Invalid_contractor_update_changes_nothing()
    {
        var contractor = AddContractor("Aquaflow");
        var lot = AddLot("09", LotFamily.BuildingServices, contractor, 538_900m);

        var result = await _service.UpdateContractorAsync(
            _admin.Id, _site.Id, contractor.Id, ValidContractor() with { CompanyName = " ", LotIds = [] });

        Assert.Equal(SiteOutcome.Invalid, result.Outcome);
        Assert.Equal("Aquaflow", contractor.CompanyName);
        Assert.Equal(contractor.Id, lot.ContractorId);
    }

    [Fact]
    public async Task Get_returns_lots_by_number_and_contractors_by_name()
    {
        var zeta = AddContractor("zeta Builders");
        var alpha = AddContractor("Alpha Paint");
        AddLot("06", LotFamily.FinishingWorks, alpha, 10m);
        AddLot("01", LotFamily.Structure, zeta, 20m);

        var site = (await _service.GetAsync(_admin.Id, _site.Id)).Value!;

        Assert.Equal(["01", "06"], site.Lots.Select(l => l.Number));
        Assert.Equal(["Alpha Paint", "zeta Builders"], site.Contractors.Select(c => c.CompanyName));
        Assert.Equal(new ContractTotalsResponse(30m, 20m, 10m, 0m, 0, 0), site.Totals);
    }

    private static SiteRequest ValidSite() => new("Linden Court housing", "12 Avenue des Tilleuls", "Cedar Homes", "North Arc", Start, Handover);

    private static LotRequest ValidLot() => new("03", "Joinery", "FinishingWorks", 142_615.80m);

    private static ContractorRequest ValidContractor() => new("Fine Finish Workshop", null, null, null, null, null, []);

    private Lot AddLot(string number, LotFamily family, Contractor? contractor, decimal? amount)
    {
        var lot = new Lot(_site.Id, number, $"Lot {number}", family);
        lot.AssignTo(contractor?.Id);
        lot.SetContractAmount(amount);
        _sites.Lots.Add(lot);

        return lot;
    }

    private Contractor AddContractor(string name)
    {
        var contractor = new Contractor(_site.Id, new ContractorDetails(name), TestClock.Now.UtcDateTime);
        _sites.Contractors.Add(contractor);

        return contractor;
    }
}

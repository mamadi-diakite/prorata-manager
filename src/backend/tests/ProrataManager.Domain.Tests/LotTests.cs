namespace ProrataManager.Domain.Tests;

public sealed class LotTests
{
    private static readonly Guid SiteId = Guid.NewGuid();

    [Fact]
    public void Creates_an_unassigned_lot_without_amount()
    {
        var lot = new Lot(SiteId, " 07b ", " Plumbing ", LotFamily.BuildingServices);

        Assert.NotEqual(Guid.Empty, lot.Id);
        Assert.Equal(SiteId, lot.SiteId);
        Assert.Equal("07B", lot.Number);
        Assert.Equal("Plumbing", lot.Name);
        Assert.Equal(LotFamily.BuildingServices, lot.Family);
        Assert.Null(lot.ContractorId);
        Assert.Null(lot.ContractAmount);
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("siteId", Assert.Throws<ArgumentException>(
            () => new Lot(Guid.Empty, "01", "Structure", LotFamily.Structure)).ParamName);
        Assert.Equal("number", Assert.Throws<ArgumentException>(
            () => new Lot(SiteId, new string('1', Lot.NumberMaxLength + 1), "Structure", LotFamily.Structure)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(
            () => new Lot(SiteId, "01", " ", LotFamily.Structure)).ParamName);
        Assert.Equal("family", Assert.Throws<ArgumentOutOfRangeException>(
            () => new Lot(SiteId, "01", "Structure", (LotFamily)5)).ParamName);
    }

    [Fact]
    public void Update_replaces_number_name_and_family()
    {
        var lot = new Lot(SiteId, "01", "Earthworks", LotFamily.Structure);

        lot.Update("02", "Painting", LotFamily.FinishingWorks);

        Assert.Equal("02", lot.Number);
        Assert.Equal("Painting", lot.Name);
        Assert.Equal(LotFamily.FinishingWorks, lot.Family);
    }

    [Fact]
    public void Update_changes_nothing_when_a_value_is_invalid()
    {
        var lot = new Lot(SiteId, "01", "Earthworks", LotFamily.Structure);

        Assert.Throws<ArgumentOutOfRangeException>(() => lot.Update("02", "Painting", (LotFamily)5));

        Assert.Equal("01", lot.Number);
        Assert.Equal("Earthworks", lot.Name);
    }

    [Fact]
    public void Assign_to_sets_or_clears_the_contractor()
    {
        var lot = new Lot(SiteId, "01", "Earthworks", LotFamily.Structure);
        var contractorId = Guid.NewGuid();

        lot.AssignTo(contractorId);
        Assert.Equal(contractorId, lot.ContractorId);

        lot.AssignTo(null);
        Assert.Null(lot.ContractorId);

        Assert.Equal("contractorId", Assert.Throws<ArgumentException>(() => lot.AssignTo(Guid.Empty)).ParamName);
    }

    [Fact]
    public void Contract_amount_is_positive_with_two_decimals()
    {
        var lot = new Lot(SiteId, "01", "Earthworks", LotFamily.Structure);

        lot.SetContractAmount(1_284_500.50m);
        Assert.Equal(1_284_500.50m, lot.ContractAmount);

        Assert.Equal("amount", Assert.Throws<ArgumentOutOfRangeException>(() => lot.SetContractAmount(0m)).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => lot.SetContractAmount(10.123m));
        Assert.Equal(1_284_500.50m, lot.ContractAmount);
    }
}

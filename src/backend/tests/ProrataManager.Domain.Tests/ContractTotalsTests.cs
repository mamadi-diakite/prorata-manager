namespace ProrataManager.Domain.Tests;

public sealed class ContractTotalsTests
{
    private static readonly Guid SiteId = Guid.NewGuid();

    [Fact]
    public void No_lots_gives_zero_everywhere()
    {
        var totals = ContractTotals.Of([]);

        Assert.Equal(0m, totals.Total);
        Assert.Equal(Enum.GetValues<LotFamily>(), totals.ByFamily.Keys.Order());
        Assert.All(totals.ByFamily.Values, amount => Assert.Equal(0m, amount));
        Assert.Empty(totals.ByContractor);
        Assert.Equal(0, totals.LotsWithoutAmount);
        Assert.Equal(0, totals.LotsWithoutContractor);
    }

    [Fact]
    public void Adds_up_amounts_by_family_and_by_contractor()
    {
        var structure = Guid.NewGuid();
        var finishes = Guid.NewGuid();
        var lots = new[]
        {
            NewLot("01", LotFamily.Structure, structure, 1_845_000.00m),
            NewLot("02", LotFamily.Structure, structure, 286_450.00m),
            NewLot("05", LotFamily.FinishingWorks, finishes, 318_720.50m),
            NewLot("06", LotFamily.FinishingWorks, finishes, null),
            NewLot("08", LotFamily.BuildingServices, null, 412_380.45m),
            NewLot("09", LotFamily.BuildingServices, null, null),
        };

        var totals = ContractTotals.Of(lots);

        Assert.Equal(2_862_550.95m, totals.Total);
        Assert.Equal(2_131_450.00m, totals.ByFamily[LotFamily.Structure]);
        Assert.Equal(318_720.50m, totals.ByFamily[LotFamily.FinishingWorks]);
        Assert.Equal(412_380.45m, totals.ByFamily[LotFamily.BuildingServices]);
        Assert.Equal(2_131_450.00m, totals.ForContractor(structure));
        Assert.Equal(318_720.50m, totals.ForContractor(finishes));
        Assert.Equal(0m, totals.ForContractor(Guid.NewGuid()));
        Assert.Equal(2, totals.LotsWithoutAmount);
        Assert.Equal(2, totals.LotsWithoutContractor);
    }

    [Fact]
    public void A_contractor_whose_lots_have_no_amount_yet_holds_zero()
    {
        var contractor = Guid.NewGuid();

        var totals = ContractTotals.Of([NewLot("03", LotFamily.FinishingWorks, contractor, null)]);

        Assert.Equal(0m, totals.ByContractor[contractor]);
    }

    [Fact]
    public void Rejects_a_missing_list()
    {
        Assert.Throws<ArgumentNullException>(() => ContractTotals.Of(null!));
    }

    private static Lot NewLot(string number, LotFamily family, Guid? contractorId, decimal? amount)
    {
        var lot = new Lot(SiteId, number, $"Lot {number}", family);
        lot.AssignTo(contractorId);
        lot.SetContractAmount(amount);

        return lot;
    }
}

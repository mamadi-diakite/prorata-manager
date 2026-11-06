namespace ProrataManager.Domain;

/// <summary>Contract amounts of a site's lots, excluding VAT, added up by family and by contractor.</summary>
/// <param name="ByContractor">Contract amount of each contractor: the sum of the lots it holds.</param>
/// <param name="LotsWithoutAmount">Lots whose amount is not recorded yet; they count as zero.</param>
public sealed record ContractTotals(
    decimal Total,
    IReadOnlyDictionary<LotFamily, decimal> ByFamily,
    IReadOnlyDictionary<Guid, decimal> ByContractor,
    int LotsWithoutAmount,
    int LotsWithoutContractor)
{
    public static ContractTotals Of(IEnumerable<Lot> lots)
    {
        ArgumentNullException.ThrowIfNull(lots);

        var byFamily = Enum.GetValues<LotFamily>().ToDictionary(family => family, _ => 0m);
        var byContractor = new Dictionary<Guid, decimal>();
        var total = 0m;
        var withoutAmount = 0;
        var withoutContractor = 0;
        foreach (var lot in lots)
        {
            var amount = lot.ContractAmount ?? 0m;
            withoutAmount += lot.ContractAmount is null ? 1 : 0;
            total += amount;
            byFamily[lot.Family] += amount;

            if (lot.ContractorId is { } contractorId)
            {
                byContractor[contractorId] = byContractor.GetValueOrDefault(contractorId) + amount;
            }
            else
            {
                withoutContractor++;
            }
        }

        return new ContractTotals(total, byFamily, byContractor, withoutAmount, withoutContractor);
    }

    public decimal ForContractor(Guid contractorId) => ByContractor.GetValueOrDefault(contractorId);
}

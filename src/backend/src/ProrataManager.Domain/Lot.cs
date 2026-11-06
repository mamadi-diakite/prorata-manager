namespace ProrataManager.Domain;

public enum LotFamily
{
    Structure = 0,
    FinishingWorks = 1,
    BuildingServices = 2,
}

/// <summary>A part of the works on a site, given to one contractor.</summary>
public sealed class Lot
{
    public const int NumberMaxLength = 10;
    public const int NameMaxLength = 150;

    private Lot()
    {
    }

    public Lot(Guid siteId, string number, string name, LotFamily family)
    {
        Id = Guid.CreateVersion7();
        SiteId = Guard.NotEmpty(siteId, nameof(siteId));
        Update(number, name, family);
    }

    public Guid Id { get; private set; }

    public Guid SiteId { get; private set; }

    /// <summary>Lot number as written in the contract documents, e.g. 01 or 07B.</summary>
    public string Number { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public LotFamily Family { get; private set; }

    /// <summary>Contractor holding the lot; null until the lot is assigned.</summary>
    public Guid? ContractorId { get; private set; }

    /// <summary>Contract amount excluding VAT, in euros; null until it is recorded.</summary>
    public decimal? ContractAmount { get; private set; }

    public void Update(string number, string name, LotFamily family)
    {
        var newNumber = Guard.Required(number, nameof(number), NumberMaxLength).ToUpperInvariant();
        var newName = Guard.Required(name, nameof(name), NameMaxLength);
        var newFamily = Guard.Defined(family, nameof(family));

        (Number, Name, Family) = (newNumber, newName, newFamily);
    }

    public void AssignTo(Guid? contractorId) =>
        ContractorId = contractorId is { } id ? Guard.NotEmpty(id, nameof(contractorId)) : null;

    /// <param name="amount">Excluding VAT; null when the amount is not known yet.</param>
    public void SetContractAmount(decimal? amount) =>
        ContractAmount = amount is { } value ? Money.Amount(value, nameof(amount)) : null;
}

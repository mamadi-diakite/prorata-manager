namespace ProrataManager.Application.Contracts.Sites;

/// <summary>Creates a site or replaces its details.</summary>
public sealed record SiteRequest(
    string Name,
    string Address,
    string ProjectOwner,
    string ProjectManager,
    DateOnly? StartDate,
    DateOnly? PlannedHandoverDate);

/// <param name="Status">Preparation, InProgress, HandedOver or Closed. A site never goes back to an earlier status.</param>
public sealed record ChangeSiteStatusRequest(string Status);

/// <param name="Family">Structure, FinishingWorks or BuildingServices.</param>
/// <param name="ContractAmount">Excluding VAT, in euros with two decimals at most; null when not known yet.</param>
public sealed record LotRequest(string Number, string Name, string Family, decimal? ContractAmount);

/// <param name="LotIds">Lots of the site the contractor holds. Lots left out are released.</param>
public sealed record ContractorRequest(
    string CompanyName,
    string? RegistrationNumber,
    string? Address,
    string? ContactName,
    string? ContactEmail,
    string? Phone,
    IReadOnlyList<Guid>? LotIds);

/// <param name="TotalContractAmount">Sum of the recorded lot amounts, excluding VAT.</param>
public sealed record SiteSummaryResponse(
    Guid Id,
    string Name,
    string Address,
    string Status,
    DateOnly StartDate,
    DateOnly PlannedHandoverDate,
    int LotCount,
    int ContractorCount,
    decimal TotalContractAmount);

/// <param name="Lots">Sorted by lot number.</param>
/// <param name="Contractors">Sorted by company name.</param>
public sealed record SiteResponse(
    Guid Id,
    string Name,
    string Address,
    string ProjectOwner,
    string ProjectManager,
    DateOnly StartDate,
    DateOnly PlannedHandoverDate,
    string Status,
    IReadOnlyList<LotResponse> Lots,
    IReadOnlyList<ContractorResponse> Contractors,
    ContractTotalsResponse Totals);

public sealed record LotResponse(
    Guid Id,
    string Number,
    string Name,
    string Family,
    Guid? ContractorId,
    string? ContractorName,
    decimal? ContractAmount);

/// <param name="ContractAmount">Sum of the amounts of the lots it holds, excluding VAT.</param>
public sealed record ContractorResponse(
    Guid Id,
    string CompanyName,
    string? RegistrationNumber,
    string? Address,
    string? ContactName,
    string? ContactEmail,
    string? Phone,
    IReadOnlyList<Guid> LotIds,
    decimal ContractAmount);

/// <summary>Contract amounts excluding VAT, in total and by lot family.</summary>
public sealed record ContractTotalsResponse(
    decimal Total,
    decimal Structure,
    decimal FinishingWorks,
    decimal BuildingServices,
    int LotsWithoutAmount,
    int LotsWithoutContractor);

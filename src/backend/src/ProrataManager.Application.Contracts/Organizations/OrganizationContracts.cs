namespace ProrataManager.Application.Contracts.Organizations;

/// <param name="Kind">"Contractor" when the manager is one of the contractors, "ThirdParty" otherwise.</param>
public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string Kind,
    string? RegistrationNumber,
    string? Address,
    string? Phone);

public sealed record UpdateOrganizationRequest(
    string Name,
    string Kind,
    string? RegistrationNumber,
    string? Address,
    string? Phone);

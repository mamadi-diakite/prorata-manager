namespace ProrataManager.Application.Organizations;

public enum OrganizationStatus
{
    Ok,
    UserNotFound,
    Invalid,
}

/// <param name="Errors">Messages by field name in camelCase when <paramref name="Status"/> is Invalid.</param>
public sealed record OrganizationResult(
    OrganizationStatus Status,
    OrganizationResponse? Organization,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static OrganizationResult Of(OrganizationStatus status) => new(status, null, new Dictionary<string, string[]>());
}

public sealed class OrganizationService(IUserRepository users)
{
    /// <returns>The organization the user belongs to.</returns>
    public async Task<OrganizationResult> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var organization = await FindOrganizationAsync(userId, cancellationToken);

        return organization is null
            ? OrganizationResult.Of(OrganizationStatus.UserNotFound)
            : new OrganizationResult(OrganizationStatus.Ok, ToResponse(organization), new Dictionary<string, string[]>());
    }

    /// <summary>Changes the details of the user's organization, including whether it is a contractor or a third party.</summary>
    public async Task<OrganizationResult> UpdateAsync(
        Guid userId,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var organization = await FindOrganizationAsync(userId, cancellationToken);
        if (organization is null)
        {
            return OrganizationResult.Of(OrganizationStatus.UserNotFound);
        }

        var errors = new FieldErrors();
        var name = errors.Required("name", request.Name, ManagerOrganization.NameMaxLength, "Organization name is required.");
        var kind = errors.Choice<ManagerKind>("kind", request.Kind, AuthService.KindMessage);
        var registrationNumber = errors.Optional(
            "registrationNumber", request.RegistrationNumber, ManagerOrganization.RegistrationNumberMaxLength);
        var address = errors.Optional("address", request.Address, ManagerOrganization.AddressMaxLength);
        var phone = errors.Optional("phone", request.Phone, ManagerOrganization.PhoneMaxLength);
        if (errors.Count > 0)
        {
            return new OrganizationResult(OrganizationStatus.Invalid, null, errors);
        }

        organization.UpdateDetails(name!, kind!.Value, registrationNumber, address, phone);
        await users.SaveChangesAsync(cancellationToken);

        return new OrganizationResult(OrganizationStatus.Ok, ToResponse(organization), new Dictionary<string, string[]>());
    }

    private async Task<ManagerOrganization?> FindOrganizationAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);

        return user is null ? null : await users.FindOrganizationAsync(user.OrganizationId, cancellationToken);
    }

    private static OrganizationResponse ToResponse(ManagerOrganization organization) => new(
        organization.Id,
        organization.Name,
        organization.Kind.ToString(),
        organization.RegistrationNumber,
        organization.Address,
        organization.Phone);
}

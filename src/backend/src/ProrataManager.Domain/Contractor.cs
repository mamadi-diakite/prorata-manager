namespace ProrataManager.Domain;

/// <summary>A company holding one or more lots on a site.</summary>
public sealed class Contractor
{
    public const int CompanyNameMaxLength = 150;
    public const int RegistrationNumberMaxLength = 30;
    public const int AddressMaxLength = 300;
    public const int ContactNameMaxLength = 100;
    public const int EmailMaxLength = 254;
    public const int PhoneMaxLength = 30;

    private Contractor()
    {
    }

    public Contractor(Guid siteId, ContractorDetails details, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        SiteId = Guard.NotEmpty(siteId, nameof(siteId));
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdateDetails(details);
    }

    public Guid Id { get; private set; }

    public Guid SiteId { get; private set; }

    public string CompanyName { get; private set; } = string.Empty;

    /// <summary>Company registration number, e.g. a SIRET number.</summary>
    public string? RegistrationNumber { get; private set; }

    public string? Address { get; private set; }

    public string? ContactName { get; private set; }

    public string? ContactEmail { get; private set; }

    public string? Phone { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static bool IsValidEmail([NotNullWhen(true)] string? email) => Guard.IsEmail(email, EmailMaxLength);

    public void UpdateDetails(ContractorDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        var companyName = Guard.Required(details.CompanyName, nameof(details.CompanyName), CompanyNameMaxLength);
        var registrationNumber = Guard.Optional(
            details.RegistrationNumber, nameof(details.RegistrationNumber), RegistrationNumberMaxLength);
        var address = Guard.Optional(details.Address, nameof(details.Address), AddressMaxLength);
        var contactName = Guard.Optional(details.ContactName, nameof(details.ContactName), ContactNameMaxLength);
        var contactEmail = Guard.OptionalEmail(details.ContactEmail, nameof(details.ContactEmail), EmailMaxLength);
        var phone = Guard.Optional(details.Phone, nameof(details.Phone), PhoneMaxLength);

        (CompanyName, RegistrationNumber, Address) = (companyName, registrationNumber, address);
        (ContactName, ContactEmail, Phone) = (contactName, contactEmail, phone);
    }
}

public sealed record ContractorDetails(
    string CompanyName,
    string? RegistrationNumber = null,
    string? Address = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? Phone = null);

namespace ProrataManager.Domain;

public enum ManagerKind
{
    Contractor = 0,
    ThirdParty = 1,
}

/// <summary>The organization that runs the shared-cost account of its sites: a contractor or a third party.</summary>
public sealed class ManagerOrganization
{
    public const int NameMaxLength = 150;
    public const int RegistrationNumberMaxLength = 30;
    public const int AddressMaxLength = 300;
    public const int PhoneMaxLength = 30;

    private ManagerOrganization()
    {
    }

    public ManagerOrganization(string name, ManagerKind kind, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        Name = Guard.Required(name, nameof(name), NameMaxLength);
        Kind = Guard.Defined(kind, nameof(kind));
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ManagerKind Kind { get; private set; }

    /// <summary>Company registration number, e.g. a SIRET number.</summary>
    public string? RegistrationNumber { get; private set; }

    public string? Address { get; private set; }

    public string? Phone { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public void UpdateDetails(string name, ManagerKind kind, string? registrationNumber, string? address, string? phone)
    {
        var newName = Guard.Required(name, nameof(name), NameMaxLength);
        var newKind = Guard.Defined(kind, nameof(kind));
        var newRegistrationNumber = Guard.Optional(registrationNumber, nameof(registrationNumber), RegistrationNumberMaxLength);
        var newAddress = Guard.Optional(address, nameof(address), AddressMaxLength);
        var newPhone = Guard.Optional(phone, nameof(phone), PhoneMaxLength);

        (Name, Kind, RegistrationNumber, Address, Phone) = (newName, newKind, newRegistrationNumber, newAddress, newPhone);
    }
}

namespace ProrataManager.Domain;

/// <remarks>A site only moves forward through these steps.</remarks>
public enum SiteStatus
{
    Preparation = 0,
    InProgress = 1,
    HandedOver = 2,
    Closed = 3,
}

/// <summary>A construction project run by a manager organization.</summary>
public sealed class Site
{
    public const int NameMaxLength = 150;
    public const int AddressMaxLength = 300;
    public const int PartyNameMaxLength = 150;

    private Site()
    {
    }

    public Site(
        Guid organizationId,
        string name,
        string address,
        string projectOwner,
        string projectManager,
        DateOnly startDate,
        DateOnly plannedHandoverDate,
        DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        OrganizationId = Guard.NotEmpty(organizationId, nameof(organizationId));
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
        Status = SiteStatus.Preparation;
        UpdateDetails(name, address, projectOwner, projectManager, startDate, plannedHandoverDate);
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    /// <summary>Name of the client who orders the works.</summary>
    public string ProjectOwner { get; private set; } = string.Empty;

    /// <summary>Name of the architect or engineer who supervises the works.</summary>
    public string ProjectManager { get; private set; } = string.Empty;

    public DateOnly StartDate { get; private set; }

    public DateOnly PlannedHandoverDate { get; private set; }

    public SiteStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static bool IsValidSchedule(DateOnly startDate, DateOnly plannedHandoverDate) => plannedHandoverDate > startDate;

    public static bool CanMove(SiteStatus from, SiteStatus to) => Enum.IsDefined(to) && to >= from;

    public void UpdateDetails(
        string name,
        string address,
        string projectOwner,
        string projectManager,
        DateOnly startDate,
        DateOnly plannedHandoverDate)
    {
        var newName = Guard.Required(name, nameof(name), NameMaxLength);
        var newAddress = Guard.Required(address, nameof(address), AddressMaxLength);
        var newOwner = Guard.Required(projectOwner, nameof(projectOwner), PartyNameMaxLength);
        var newManager = Guard.Required(projectManager, nameof(projectManager), PartyNameMaxLength);
        if (!IsValidSchedule(startDate, plannedHandoverDate))
        {
            throw new ArgumentException("Planned handover must be after the start date.", nameof(plannedHandoverDate));
        }

        (Name, Address, ProjectOwner, ProjectManager) = (newName, newAddress, newOwner, newManager);
        (StartDate, PlannedHandoverDate) = (startDate, plannedHandoverDate);
    }

    public void ChangeStatus(SiteStatus status)
    {
        if (!CanMove(Status, status))
        {
            throw new InvalidOperationException($"A site cannot move from {Status} to {status}.");
        }

        Status = status;
    }
}

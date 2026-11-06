namespace ProrataManager.Application.Sites;

public enum SiteOutcome
{
    Ok,
    UserNotFound,
    NotFound,
    Invalid,
}

/// <param name="Errors">Messages by field name in camelCase when <paramref name="Outcome"/> is Invalid.</param>
public sealed record SiteResult<T>(SiteOutcome Outcome, T? Value, IReadOnlyDictionary<string, string[]> Errors)
    where T : class
{
    public static SiteResult<T> Of(SiteOutcome outcome) => new(outcome, null, new Dictionary<string, string[]>());

    public static SiteResult<T> Ok(T value) => new(SiteOutcome.Ok, value, new Dictionary<string, string[]>());

    public static SiteResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors) => new(SiteOutcome.Invalid, null, errors);
}

/// <summary>Sites of the user's organization, with their lots, contractors and contract amounts.</summary>
public sealed class SiteService(IUserRepository users, ISiteRepository sites, TimeProvider timeProvider)
{
    public const string AmountMessage = "Enter a positive amount in euros, with two decimals at most.";
    public const string FamilyMessage = "Choose a family: structure, finishing works or building services.";

    public async Task<SiteResult<IReadOnlyList<SiteSummaryResponse>>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return SiteResult<IReadOnlyList<SiteSummaryResponse>>.Of(SiteOutcome.UserNotFound);
        }

        var list = await sites.ListAsync(user.OrganizationId, cancellationToken);
        var siteIds = list.Select(s => s.Id).ToList();
        var lots = await sites.ListLotsAsync(siteIds, cancellationToken);
        var contractors = await sites.ListContractorsAsync(siteIds, cancellationToken);

        return SiteResult<IReadOnlyList<SiteSummaryResponse>>.Ok(
        [
            .. list.Select(site =>
            {
                var siteLots = lots.Where(l => l.SiteId == site.Id).ToList();

                return new SiteSummaryResponse(
                    site.Id,
                    site.Name,
                    site.Address,
                    site.Status.ToString(),
                    site.StartDate,
                    site.PlannedHandoverDate,
                    siteLots.Count,
                    contractors.Count(c => c.SiteId == site.Id),
                    ContractTotals.Of(siteLots).Total);
            }),
        ]);
    }

    public async Task<SiteResult<SiteResponse>> GetAsync(Guid userId, Guid siteId, CancellationToken cancellationToken = default)
    {
        var (outcome, data) = await LoadAsync(userId, siteId, cancellationToken);

        return outcome == SiteOutcome.Ok ? SiteResult<SiteResponse>.Ok(ToResponse(data!)) : SiteResult<SiteResponse>.Of(outcome);
    }

    /// <summary>Creates a site in preparation for the user's organization.</summary>
    public async Task<SiteResult<SiteResponse>> CreateAsync(Guid userId, SiteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return SiteResult<SiteResponse>.Of(SiteOutcome.UserNotFound);
        }

        var errors = new FieldErrors();
        if (ReadDetails(errors, request) is not { } details)
        {
            return SiteResult<SiteResponse>.Invalid(errors);
        }

        var site = new Site(
            user.OrganizationId,
            details.Name,
            details.Address,
            details.ProjectOwner,
            details.ProjectManager,
            details.StartDate,
            details.PlannedHandoverDate,
            timeProvider.GetUtcNow().UtcDateTime);
        sites.Add(site);
        await sites.SaveChangesAsync(cancellationToken);

        return SiteResult<SiteResponse>.Ok(ToResponse(new SiteData(site, [], [])));
    }

    public async Task<SiteResult<SiteResponse>> UpdateAsync(
        Guid userId,
        Guid siteId,
        SiteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await ChangeAsync(userId, siteId, (data, errors) =>
        {
            if (ReadDetails(errors, request) is { } details)
            {
                data.Site.UpdateDetails(
                    details.Name, details.Address, details.ProjectOwner, details.ProjectManager, details.StartDate, details.PlannedHandoverDate);
            }

            return SiteOutcome.Ok;
        }, cancellationToken);
    }

    /// <summary>Moves the site to a later status: preparation, in progress, handed over, closed.</summary>
    public async Task<SiteResult<SiteResponse>> ChangeStatusAsync(
        Guid userId,
        Guid siteId,
        ChangeSiteStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await ChangeAsync(userId, siteId, (data, errors) =>
        {
            var status = errors.Choice<SiteStatus>("status", request.Status, "Choose a status.");
            if (status is not { } next)
            {
                return SiteOutcome.Ok;
            }

            if (!Site.CanMove(data.Site.Status, next))
            {
                errors["status"] = ["A site cannot go back to an earlier status."];
                return SiteOutcome.Ok;
            }

            data.Site.ChangeStatus(next);
            return SiteOutcome.Ok;
        }, cancellationToken);
    }

    public async Task<SiteResult<SiteResponse>> AddLotAsync(
        Guid userId,
        Guid siteId,
        LotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await ChangeAsync(userId, siteId, (data, errors) =>
        {
            if (ReadLot(errors, request, data.Lots, lotId: null) is { } values)
            {
                var lot = new Lot(data.Site.Id, values.Number, values.Name, values.Family);
                lot.SetContractAmount(values.ContractAmount);
                sites.Add(lot);
                data.Lots.Add(lot);
            }

            return SiteOutcome.Ok;
        }, cancellationToken);
    }

    /// <summary>Changes a lot's number, name, family or contract amount.</summary>
    public async Task<SiteResult<SiteResponse>> UpdateLotAsync(
        Guid userId,
        Guid siteId,
        Guid lotId,
        LotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await ChangeAsync(userId, siteId, (data, errors) =>
        {
            if (data.Lots.Find(l => l.Id == lotId) is not { } lot)
            {
                return SiteOutcome.NotFound;
            }

            if (ReadLot(errors, request, data.Lots, lotId) is { } values)
            {
                lot.Update(values.Number, values.Name, values.Family);
                lot.SetContractAmount(values.ContractAmount);
            }

            return SiteOutcome.Ok;
        }, cancellationToken);
    }

    /// <summary>Adds a contractor with its company details and gives it the chosen lots.</summary>
    public async Task<SiteResult<SiteResponse>> AddContractorAsync(
        Guid userId,
        Guid siteId,
        ContractorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await ChangeAsync(userId, siteId, (data, errors) =>
        {
            if (ReadContractor(errors, request, data, contractorId: null) is { } values)
            {
                var contractor = new Contractor(data.Site.Id, values.Details, timeProvider.GetUtcNow().UtcDateTime);
                sites.Add(contractor);
                data.Contractors.Add(contractor);
                values.Lots.ForEach(lot => lot.AssignTo(contractor.Id));
            }

            return SiteOutcome.Ok;
        }, cancellationToken);
    }

    /// <summary>Replaces a contractor's details and the lots it holds; lots left out are released.</summary>
    public async Task<SiteResult<SiteResponse>> UpdateContractorAsync(
        Guid userId,
        Guid siteId,
        Guid contractorId,
        ContractorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await ChangeAsync(userId, siteId, (data, errors) =>
        {
            if (data.Contractors.Find(c => c.Id == contractorId) is not { } contractor)
            {
                return SiteOutcome.NotFound;
            }

            if (ReadContractor(errors, request, data, contractorId) is { } values)
            {
                contractor.UpdateDetails(values.Details);
                data.Lots.Where(l => l.ContractorId == contractorId && !values.Lots.Contains(l)).ToList().ForEach(l => l.AssignTo(null));
                values.Lots.ForEach(lot => lot.AssignTo(contractorId));
            }

            return SiteOutcome.Ok;
        }, cancellationToken);
    }

    /// <summary>Loads the site, applies the change unless it reports errors, then saves.</summary>
    private async Task<SiteResult<SiteResponse>> ChangeAsync(
        Guid userId,
        Guid siteId,
        Func<SiteData, FieldErrors, SiteOutcome> change,
        CancellationToken cancellationToken)
    {
        var (outcome, data) = await LoadAsync(userId, siteId, cancellationToken);
        if (outcome != SiteOutcome.Ok)
        {
            return SiteResult<SiteResponse>.Of(outcome);
        }

        var errors = new FieldErrors();
        outcome = change(data!, errors);
        if (outcome != SiteOutcome.Ok)
        {
            return SiteResult<SiteResponse>.Of(outcome);
        }

        if (errors.Count > 0)
        {
            return SiteResult<SiteResponse>.Invalid(errors);
        }

        await sites.SaveChangesAsync(cancellationToken);

        return SiteResult<SiteResponse>.Ok(ToResponse(data!));
    }

    private async Task<(SiteOutcome Outcome, SiteData? Data)> LoadAsync(Guid userId, Guid siteId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return (SiteOutcome.UserNotFound, null);
        }

        var site = await sites.FindAsync(user.OrganizationId, siteId, cancellationToken);
        if (site is null)
        {
            return (SiteOutcome.NotFound, null);
        }

        var lots = await sites.ListLotsAsync([site.Id], cancellationToken);
        var contractors = await sites.ListContractorsAsync([site.Id], cancellationToken);

        return (SiteOutcome.Ok, new SiteData(site, [.. lots], [.. contractors]));
    }

    private static SiteDetails? ReadDetails(FieldErrors errors, SiteRequest request)
    {
        var name = errors.Required("name", request.Name, Site.NameMaxLength, "Site name is required.");
        var address = errors.Required("address", request.Address, Site.AddressMaxLength, "Address is required.");
        var owner = errors.Required("projectOwner", request.ProjectOwner, Site.PartyNameMaxLength, "Project owner is required.");
        var manager = errors.Required("projectManager", request.ProjectManager, Site.PartyNameMaxLength, "Project manager is required.");
        if (request.StartDate is null)
        {
            errors["startDate"] = ["Start date is required."];
        }

        if (request.PlannedHandoverDate is null)
        {
            errors["plannedHandoverDate"] = ["Planned handover date is required."];
        }
        else if (request.StartDate is { } start && !Site.IsValidSchedule(start, request.PlannedHandoverDate.Value))
        {
            errors["plannedHandoverDate"] = ["Planned handover must be after the start date."];
        }

        return errors.Count == 0
            ? new SiteDetails(name!, address!, owner!, manager!, request.StartDate!.Value, request.PlannedHandoverDate!.Value)
            : null;
    }

    private static LotValues? ReadLot(FieldErrors errors, LotRequest request, List<Lot> lots, Guid? lotId)
    {
        var number = errors.Required("number", request.Number, Lot.NumberMaxLength, "Lot number is required.")?.ToUpperInvariant();
        if (number is not null && lots.Exists(l => l.Id != lotId && l.Number == number))
        {
            errors["number"] = ["Another lot of this site already uses this number."];
        }

        var name = errors.Required("name", request.Name, Lot.NameMaxLength, "Lot name is required.");
        var family = errors.Choice<LotFamily>("family", request.Family, FamilyMessage);
        if (request.ContractAmount is { } amount && !Money.IsValidAmount(amount))
        {
            errors["contractAmount"] = [AmountMessage];
        }

        return errors.Count == 0 ? new LotValues(number!, name!, family!.Value, request.ContractAmount) : null;
    }

    private static ContractorValues? ReadContractor(FieldErrors errors, ContractorRequest request, SiteData data, Guid? contractorId)
    {
        var companyName = errors.Required("companyName", request.CompanyName, Contractor.CompanyNameMaxLength, "Company name is required.");
        var registrationNumber = errors.Optional("registrationNumber", request.RegistrationNumber, Contractor.RegistrationNumberMaxLength);
        var address = errors.Optional("address", request.Address, Contractor.AddressMaxLength);
        var contactName = errors.Optional("contactName", request.ContactName, Contractor.ContactNameMaxLength);
        var phone = errors.Optional("phone", request.Phone, Contractor.PhoneMaxLength);
        if (!string.IsNullOrWhiteSpace(request.ContactEmail) && !Contractor.IsValidEmail(request.ContactEmail))
        {
            errors["contactEmail"] = ["Enter a valid email address."];
        }

        var lots = new List<Lot>();
        var lotErrors = new List<string>();
        foreach (var lotId in (request.LotIds ?? []).Distinct())
        {
            var lot = data.Lots.Find(l => l.Id == lotId);
            if (lot is null)
            {
                lotErrors.Add("Choose lots of this site.");
            }
            else if (lot.ContractorId is { } holderId && holderId != contractorId)
            {
                var holder = data.Contractors.First(c => c.Id == holderId);
                lotErrors.Add($"Lot {lot.Number} is already held by {holder.CompanyName}.");
            }
            else
            {
                lots.Add(lot);
            }
        }

        if (lotErrors.Count > 0)
        {
            errors["lotIds"] = [.. lotErrors.Distinct()];
        }

        return errors.Count == 0
            ? new ContractorValues(
                new ContractorDetails(companyName!, registrationNumber, address, contactName, request.ContactEmail, phone), lots)
            : null;
    }

    private static SiteResponse ToResponse(SiteData data)
    {
        var totals = ContractTotals.Of(data.Lots);
        var names = data.Contractors.ToDictionary(c => c.Id, c => c.CompanyName);
        var lots = data.Lots.OrderBy(l => l.Number, StringComparer.Ordinal).ToList();

        return new SiteResponse(
            data.Site.Id,
            data.Site.Name,
            data.Site.Address,
            data.Site.ProjectOwner,
            data.Site.ProjectManager,
            data.Site.StartDate,
            data.Site.PlannedHandoverDate,
            data.Site.Status.ToString(),
            [
                .. lots.Select(l => new LotResponse(
                    l.Id,
                    l.Number,
                    l.Name,
                    l.Family.ToString(),
                    l.ContractorId,
                    l.ContractorId is { } id ? names[id] : null,
                    l.ContractAmount)),
            ],
            [
                .. data.Contractors.OrderBy(c => c.CompanyName, StringComparer.OrdinalIgnoreCase).Select(c => new ContractorResponse(
                    c.Id,
                    c.CompanyName,
                    c.RegistrationNumber,
                    c.Address,
                    c.ContactName,
                    c.ContactEmail,
                    c.Phone,
                    [.. lots.Where(l => l.ContractorId == c.Id).Select(l => l.Id)],
                    totals.ForContractor(c.Id))),
            ],
            new ContractTotalsResponse(
                totals.Total,
                totals.ByFamily[LotFamily.Structure],
                totals.ByFamily[LotFamily.FinishingWorks],
                totals.ByFamily[LotFamily.BuildingServices],
                totals.LotsWithoutAmount,
                totals.LotsWithoutContractor));
    }

    private sealed record SiteData(Site Site, List<Lot> Lots, List<Contractor> Contractors);

    private sealed record SiteDetails(
        string Name,
        string Address,
        string ProjectOwner,
        string ProjectManager,
        DateOnly StartDate,
        DateOnly PlannedHandoverDate);

    private sealed record LotValues(string Number, string Name, LotFamily Family, decimal? ContractAmount);

    private sealed record ContractorValues(ContractorDetails Details, List<Lot> Lots);
}

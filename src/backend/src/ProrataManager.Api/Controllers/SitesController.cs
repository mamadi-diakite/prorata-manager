namespace ProrataManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sites")]
public sealed class SitesController(SiteService siteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteSummaryResponse>>> List(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await siteService.ListAsync(userId, cancellationToken);

        return result.Outcome == SiteOutcome.Ok ? Ok(result.Value) : Refused(result);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.ManagerAdmin))]
    public async Task<ActionResult<SiteResponse>> Create(SiteRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await siteService.CreateAsync(userId, request, cancellationToken);

        return result.Outcome == SiteOutcome.Ok
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : Refused(result);
    }

    [HttpGet("{id:guid}")]
    public Task<ActionResult<SiteResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Run(userId => siteService.GetAsync(userId, id, cancellationToken));

    [HttpPut("{id:guid}")]
    public Task<ActionResult<SiteResponse>> Update(Guid id, SiteRequest request, CancellationToken cancellationToken) =>
        Run(userId => siteService.UpdateAsync(userId, id, request, cancellationToken));

    [HttpPut("{id:guid}/status")]
    public Task<ActionResult<SiteResponse>> ChangeStatus(Guid id, ChangeSiteStatusRequest request, CancellationToken cancellationToken) =>
        Run(userId => siteService.ChangeStatusAsync(userId, id, request, cancellationToken));

    [HttpPost("{id:guid}/lots")]
    public Task<ActionResult<SiteResponse>> AddLot(Guid id, LotRequest request, CancellationToken cancellationToken) =>
        Run(userId => siteService.AddLotAsync(userId, id, request, cancellationToken));

    [HttpPut("{id:guid}/lots/{lotId:guid}")]
    public Task<ActionResult<SiteResponse>> UpdateLot(Guid id, Guid lotId, LotRequest request, CancellationToken cancellationToken) =>
        Run(userId => siteService.UpdateLotAsync(userId, id, lotId, request, cancellationToken));

    [HttpPost("{id:guid}/contractors")]
    public Task<ActionResult<SiteResponse>> AddContractor(Guid id, ContractorRequest request, CancellationToken cancellationToken) =>
        Run(userId => siteService.AddContractorAsync(userId, id, request, cancellationToken));

    [HttpPut("{id:guid}/contractors/{contractorId:guid}")]
    public Task<ActionResult<SiteResponse>> UpdateContractor(
        Guid id,
        Guid contractorId,
        ContractorRequest request,
        CancellationToken cancellationToken) =>
        Run(userId => siteService.UpdateContractorAsync(userId, id, contractorId, request, cancellationToken));

    /// <summary>Runs a site action for the logged-in user and returns the updated site.</summary>
    private async Task<ActionResult<SiteResponse>> Run(Func<Guid, Task<SiteResult<SiteResponse>>> action)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await action(userId);

        return result.Outcome == SiteOutcome.Ok ? Ok(result.Value) : Refused(result);
    }

    private ActionResult Refused<T>(SiteResult<T> result)
        where T : class => result.Outcome switch
    {
        SiteOutcome.UserNotFound => Unauthorized(),
        SiteOutcome.NotFound => NotFound(),
        _ => this.InvalidRequest(result.Errors),
    };
}

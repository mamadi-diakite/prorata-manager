namespace ProrataManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organization")]
public sealed class OrganizationController(OrganizationService organizationService) : ControllerBase
{
    /// <summary>The manager organization of the logged-in user.</summary>
    [HttpGet]
    public async Task<ActionResult<OrganizationResponse>> Get(CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? ToActionResult(await organizationService.GetAsync(userId, cancellationToken))
            : Unauthorized();

    [HttpPut]
    [Authorize(Roles = nameof(UserRole.ManagerAdmin))]
    public async Task<ActionResult<OrganizationResponse>> Update(
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? ToActionResult(await organizationService.UpdateAsync(userId, request, cancellationToken))
            : Unauthorized();

    private ActionResult<OrganizationResponse> ToActionResult(OrganizationResult result) => result.Status switch
    {
        OrganizationStatus.Ok => Ok(result.Organization),
        OrganizationStatus.UserNotFound => Unauthorized(),
        _ => this.InvalidRequest(result.Errors),
    };
}

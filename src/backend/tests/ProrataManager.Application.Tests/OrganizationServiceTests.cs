namespace ProrataManager.Application.Tests;

public sealed class OrganizationServiceTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly OrganizationService _service;
    private readonly ManagerOrganization _organization;
    private readonly User _admin;

    public OrganizationServiceTests()
    {
        _service = new OrganizationService(_users);
        _organization = new ManagerOrganization("Atlas Structures", ManagerKind.Contractor, TestClock.Now.UtcDateTime);
        _users.Organizations.Add(_organization);
        _admin = new User(_organization.Id, "claire@example.com", "Claire", "hash", UserRole.ManagerAdmin, TestClock.Now.UtcDateTime);
        _users.Users.Add(_admin);
    }

    [Fact]
    public async Task Returns_the_organization_of_the_user()
    {
        var result = await _service.GetAsync(_admin.Id);

        Assert.Equal(OrganizationStatus.Ok, result.Status);
        Assert.Equal(new OrganizationResponse(_organization.Id, "Atlas Structures", "Contractor", null, null, null), result.Organization);
    }

    [Fact]
    public async Task Unknown_user_or_missing_organization_is_not_found()
    {
        var orphan = new User(Guid.NewGuid(), "orphan@example.com", "Orphan", "hash", UserRole.ManagerAdmin, TestClock.Now.UtcDateTime);
        _users.Users.Add(orphan);

        Assert.Equal(OrganizationStatus.UserNotFound, (await _service.GetAsync(Guid.NewGuid())).Status);
        Assert.Equal(OrganizationStatus.UserNotFound, (await _service.GetAsync(orphan.Id)).Status);
        Assert.Equal(OrganizationStatus.UserNotFound, (await _service.UpdateAsync(Guid.NewGuid(), Valid())).Status);
    }

    [Fact]
    public async Task Update_saves_the_details_and_the_kind()
    {
        var result = await _service.UpdateAsync(
            _admin.Id, new UpdateOrganizationRequest(" Prism Site Services ", "thirdparty", " 000 000 000 00012 ", " 8 Rue de l'Exemple ", " "));

        Assert.Equal(OrganizationStatus.Ok, result.Status);
        Assert.Equal(
            new OrganizationResponse(_organization.Id, "Prism Site Services", "ThirdParty", "000 000 000 00012", "8 Rue de l'Exemple", null),
            result.Organization);
        Assert.Equal(ManagerKind.ThirdParty, _organization.Kind);
        Assert.Equal(1, _users.SaveCount);
    }

    [Fact]
    public async Task Update_reports_every_invalid_field_and_changes_nothing()
    {
        var tooLong = new string('9', 301);

        var result = await _service.UpdateAsync(_admin.Id, new UpdateOrganizationRequest(" ", "Supplier", tooLong, tooLong, tooLong));

        Assert.Equal(OrganizationStatus.Invalid, result.Status);
        Assert.Null(result.Organization);
        Assert.Equal(["address", "kind", "name", "phone", "registrationNumber"], result.Errors.Keys.Order());
        Assert.Equal("Atlas Structures", _organization.Name);
        Assert.Equal(0, _users.SaveCount);
    }

    [Fact]
    public async Task Update_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(_admin.Id, null!));
    }

    private static UpdateOrganizationRequest Valid() => new("Atlas Structures", "Contractor", null, null, null);
}

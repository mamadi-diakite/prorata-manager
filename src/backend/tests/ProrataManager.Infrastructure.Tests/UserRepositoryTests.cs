namespace ProrataManager.Infrastructure.Tests;

public sealed class UserRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly ManagerOrganization _organization = new("Atlas Structures", ManagerKind.Contractor, Now);
    private readonly User _user;

    public UserRepositoryTests()
    {
        _user = new User(_organization.Id, "claire.martin@example.com", "Claire", "hash", UserRole.ManagerAdmin, Now);
        using var db = _database.CreateContext();
        db.Organizations.Add(_organization);
        db.Users.Add(_user);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Finds_a_user_by_email_and_by_id_and_its_organization()
    {
        await using var db = _database.CreateContext();
        var repository = new UserRepository(db);

        Assert.Equal(_user.Id, (await repository.FindByEmailAsync("claire.martin@example.com", default))?.Id);
        Assert.Equal(_user.Email, (await repository.FindByIdAsync(_user.Id, default))?.Email);
        Assert.Equal("Atlas Structures", (await repository.FindOrganizationAsync(_organization.Id, default))?.Name);
        Assert.Null(await repository.FindByEmailAsync("nobody@example.com", default));
        Assert.Null(await repository.FindByIdAsync(Guid.NewGuid(), default));
        Assert.Null(await repository.FindOrganizationAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Adds_an_organization_with_its_admin_and_saves_changes()
    {
        var organization = new ManagerOrganization("Prism Site Services", ManagerKind.ThirdParty, Now);
        var admin = new User(organization.Id, "hugo@example.com", "Hugo", "hash", UserRole.ManagerAdmin, Now);
        await using (var db = _database.CreateContext())
        {
            var repository = new UserRepository(db);
            repository.AddOrganization(organization);
            repository.Add(admin);
            (await repository.FindByIdAsync(_user.Id, default))!.ChangePassword("new-hash");
            (await repository.FindOrganizationAsync(_organization.Id, default))!
                .UpdateDetails("Atlas Structures", ManagerKind.Contractor, "000 000 000 00012", null, null);
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        Assert.Equal(organization.Id, (await check.Users.SingleAsync(u => u.Id == admin.Id)).OrganizationId);
        Assert.Equal("new-hash", (await check.Users.SingleAsync(u => u.Id == _user.Id)).PasswordHash);
        Assert.Equal("000 000 000 00012", (await check.Organizations.SingleAsync(o => o.Id == _organization.Id)).RegistrationNumber);
    }
}

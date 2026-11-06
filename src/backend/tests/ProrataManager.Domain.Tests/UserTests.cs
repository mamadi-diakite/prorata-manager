namespace ProrataManager.Domain.Tests;

public sealed class UserTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void Creates_a_manager_admin_in_an_organization()
    {
        var user = new User(OrganizationId, "  Claire.Martin@Example.com ", " Claire Martin ", "hash", UserRole.ManagerAdmin, TestDates.Now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(OrganizationId, user.OrganizationId);
        Assert.Equal("claire.martin@example.com", user.Email);
        Assert.Equal("Claire Martin", user.DisplayName);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal(UserRole.ManagerAdmin, user.Role);
        Assert.Equal(TestDates.Now, user.CreatedAtUtc);
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("organizationId", Assert.Throws<ArgumentException>(
            () => new User(Guid.Empty, "a@example.com", "Claire", "hash", UserRole.ManagerAdmin, TestDates.Now)).ParamName);
        Assert.Equal("email", Assert.Throws<ArgumentException>(
            () => new User(OrganizationId, "claire@", "Claire", "hash", UserRole.ManagerAdmin, TestDates.Now)).ParamName);
        Assert.Equal("displayName", Assert.Throws<ArgumentException>(
            () => new User(OrganizationId, "a@example.com", " ", "hash", UserRole.ManagerAdmin, TestDates.Now)).ParamName);
        Assert.Equal("passwordHash", Assert.Throws<ArgumentException>(
            () => new User(OrganizationId, "a@example.com", "Claire", "", UserRole.ManagerAdmin, TestDates.Now)).ParamName);
        Assert.Equal("role", Assert.Throws<ArgumentOutOfRangeException>(
            () => new User(OrganizationId, "a@example.com", "Claire", "hash", (UserRole)9, TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new User(OrganizationId, "a@example.com", "Claire", "hash", UserRole.ManagerAdmin, TestDates.Local)).ParamName);
    }

    [Theory]
    [InlineData("claire@example.com", true)]
    [InlineData("claire@", false)]
    [InlineData(null, false)]
    public void Is_valid_email_matches_the_constructor_rule(string? email, bool expected)
    {
        Assert.Equal(expected, User.IsValidEmail(email));
    }

    [Fact]
    public void Change_password_replaces_the_hash()
    {
        var user = new User(OrganizationId, "claire@example.com", "Claire", "old-hash", UserRole.ManagerAdmin, TestDates.Now);

        user.ChangePassword("new-hash");

        Assert.Equal("new-hash", user.PasswordHash);
    }

    [Fact]
    public void Change_password_rejects_a_blank_hash()
    {
        var user = new User(OrganizationId, "claire@example.com", "Claire", "old-hash", UserRole.ManagerAdmin, TestDates.Now);

        var error = Assert.Throws<ArgumentException>(() => user.ChangePassword(" "));

        Assert.Equal("passwordHash", error.ParamName);
        Assert.Equal("old-hash", user.PasswordHash);
    }
}

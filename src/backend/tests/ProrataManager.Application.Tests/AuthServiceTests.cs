namespace ProrataManager.Application.Tests;

public sealed class AuthServiceTests
{
    private const string Password = "TestDemo123!";

    private readonly InMemoryUserRepository _users = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly AuthService _service;
    private readonly User _claire;

    public AuthServiceTests()
    {
        _service = new AuthService(_users, _hasher, new FakeTokenIssuer(), new FixedTimeProvider(TestClock.Now));
        var organization = new ManagerOrganization("Atlas Structures", ManagerKind.Contractor, TestClock.Now.UtcDateTime);
        _users.Organizations.Add(organization);
        _claire = new User(
            organization.Id, "claire.martin@example.com", "Claire Martin", "hashed:" + Password, UserRole.ManagerAdmin, TestClock.Now.UtcDateTime);
        _users.Users.Add(_claire);
    }

    [Fact]
    public async Task Sign_up_creates_the_organization_and_its_admin_and_opens_a_session()
    {
        var result = await _service.SignUpAsync(new SignUpRequest(
            " Prism Site Services ", "thirdParty", " Hugo Lambert ", " Hugo.Lambert@Example.com ", "NewPass456", "NewPass456"));

        Assert.Empty(result.Errors);
        var organization = Assert.Single(_users.Organizations, o => o.Name == "Prism Site Services");
        Assert.Equal(ManagerKind.ThirdParty, organization.Kind);
        Assert.Equal(TestClock.Now.UtcDateTime, organization.CreatedAtUtc);
        var user = Assert.Single(_users.Users, u => u.Email == "hugo.lambert@example.com");
        Assert.Equal("Hugo Lambert", user.DisplayName);
        Assert.Equal(UserRole.ManagerAdmin, user.Role);
        Assert.Equal(organization.Id, user.OrganizationId);
        Assert.Equal("hashed:NewPass456", user.PasswordHash);
        Assert.Equal(1, _users.SaveCount);
        Assert.Equal($"token-for-{user.Id}", result.Response?.AccessToken);
        Assert.Equal(
            new CurrentUserResponse(user.Id, user.Email, "Hugo Lambert", "ManagerAdmin", organization.Id),
            result.Response?.User);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Sign_up_requires_an_organization_name(string organizationName)
    {
        var result = await _service.SignUpAsync(SignUp(organizationName: organizationName));

        AssertSignUpInvalid(result, "organizationName");
        Assert.Equal(["Organization name is required."], result.Errors["organizationName"]);
    }

    [Fact]
    public async Task Sign_up_limits_the_organization_name_length()
    {
        var result = await _service.SignUpAsync(SignUp(organizationName: new string('a', ManagerOrganization.NameMaxLength + 1)));

        AssertSignUpInvalid(result, "organizationName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Supplier")]
    public async Task Sign_up_requires_the_organization_kind(string kind)
    {
        var result = await _service.SignUpAsync(SignUp(kind: kind));

        AssertSignUpInvalid(result, "organizationKind");
        Assert.Equal([AuthService.KindMessage], result.Errors["organizationKind"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Sign_up_requires_a_name(string displayName)
    {
        var result = await _service.SignUpAsync(SignUp(displayName: displayName));

        AssertSignUpInvalid(result, "displayName");
        Assert.Equal(["Name is required."], result.Errors["displayName"]);
    }

    [Fact]
    public async Task Sign_up_limits_the_name_length()
    {
        var result = await _service.SignUpAsync(SignUp(displayName: new string('a', User.DisplayNameMaxLength + 1)));

        AssertSignUpInvalid(result, "displayName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Sign_up_requires_a_valid_email(string email)
    {
        var result = await _service.SignUpAsync(SignUp(email: email));

        AssertSignUpInvalid(result, "email");
        Assert.Equal(["Enter a valid email address."], result.Errors["email"]);
    }

    [Fact]
    public async Task Sign_up_refuses_an_email_already_used()
    {
        var result = await _service.SignUpAsync(SignUp(email: "CLAIRE.MARTIN@example.com"));

        AssertSignUpInvalid(result, "email");
        Assert.Equal(["An account already uses this email."], result.Errors["email"]);
    }

    [Fact]
    public async Task Sign_up_applies_the_password_policy_and_confirmation()
    {
        var result = await _service.SignUpAsync(SignUp(password: "short", confirmPassword: "other"));

        AssertSignUpInvalid(result, "password");
        Assert.Equal(["confirmPassword", "password"], result.Errors.Keys.Order());
    }

    [Fact]
    public async Task Sign_up_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SignUpAsync(null!));
    }

    [Fact]
    public async Task Login_returns_a_token_and_the_user()
    {
        var response = await _service.LoginAsync(new LoginRequest(" Claire.Martin@Example.com ", Password));

        Assert.NotNull(response);
        Assert.Equal($"token-for-{_claire.Id}", response.AccessToken);
        Assert.Equal(FakeTokenIssuer.ExpiresAt, response.ExpiresAtUtc);
        Assert.Equal(
            new CurrentUserResponse(_claire.Id, "claire.martin@example.com", "Claire Martin", "ManagerAdmin", _claire.OrganizationId),
            response.User);
    }

    [Fact]
    public async Task Login_fails_with_a_wrong_password()
    {
        Assert.Null(await _service.LoginAsync(new LoginRequest("claire.martin@example.com", "wrong-password1")));
    }

    [Fact]
    public async Task Login_fails_with_an_unknown_email_and_still_hashes()
    {
        var response = await _service.LoginAsync(new LoginRequest("nobody@example.com", Password));

        Assert.Null(response);
        Assert.Equal(1, _hasher.HashCount);
    }

    [Theory]
    [InlineData("", Password)]
    [InlineData("  ", Password)]
    [InlineData("claire.martin@example.com", "")]
    public async Task Login_fails_with_blank_values(string email, string password)
    {
        Assert.Null(await _service.LoginAsync(new LoginRequest(email, password)));
        Assert.Equal(0, _hasher.HashCount);
    }

    [Fact]
    public async Task Login_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.LoginAsync(null!));
    }

    [Fact]
    public async Task Current_user_is_found_by_id()
    {
        var user = await _service.GetCurrentUserAsync(_claire.Id);

        Assert.Equal("claire.martin@example.com", user?.Email);
        Assert.Null(await _service.GetCurrentUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Change_password_stores_the_new_hash()
    {
        var result = await _service.ChangePasswordAsync(_claire.Id, new ChangePasswordRequest(Password, "NewPass456", "NewPass456"));

        Assert.Equal(ChangePasswordStatus.Changed, result.Status);
        Assert.Empty(result.Errors);
        Assert.Equal("hashed:NewPass456", _claire.PasswordHash);
        Assert.Equal(1, _users.SaveCount);
    }

    [Fact]
    public async Task Change_password_for_an_unknown_user()
    {
        var result = await _service.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequest(Password, "NewPass456", "NewPass456"));

        Assert.Equal(ChangePasswordStatus.UserNotFound, result.Status);
        Assert.Equal(0, _users.SaveCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("WrongPass789")]
    public async Task Change_password_checks_the_current_password(string currentPassword)
    {
        var result = await _service.ChangePasswordAsync(
            _claire.Id, new ChangePasswordRequest(currentPassword, "NewPass456", "NewPass456"));

        AssertChangeInvalid(result, "currentPassword");
    }

    [Fact]
    public async Task Change_password_applies_the_password_policy()
    {
        var result = await _service.ChangePasswordAsync(_claire.Id, new ChangePasswordRequest(Password, "short", "short"));

        AssertChangeInvalid(result, "newPassword");
        Assert.Contains("Use at least 8 characters.", result.Errors["newPassword"]);
    }

    [Fact]
    public async Task New_password_must_differ_from_the_current_one()
    {
        var result = await _service.ChangePasswordAsync(_claire.Id, new ChangePasswordRequest(Password, Password, Password));

        AssertChangeInvalid(result, "newPassword");
        Assert.Equal(["New password must be different from the current one."], result.Errors["newPassword"]);
    }

    [Fact]
    public async Task Confirmation_must_match()
    {
        var result = await _service.ChangePasswordAsync(_claire.Id, new ChangePasswordRequest(Password, "NewPass456", "NewPass457"));

        AssertChangeInvalid(result, "confirmPassword");
    }

    [Fact]
    public async Task All_errors_are_reported_together()
    {
        var result = await _service.ChangePasswordAsync(_claire.Id, new ChangePasswordRequest("bad", "short", "other"));

        Assert.Equal(["confirmPassword", "currentPassword", "newPassword"], result.Errors.Keys.Order());
    }

    [Fact]
    public async Task Change_password_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ChangePasswordAsync(_claire.Id, null!));
    }

    private static SignUpRequest SignUp(
        string organizationName = "Prism Site Services",
        string kind = "ThirdParty",
        string displayName = "Hugo Lambert",
        string email = "hugo.lambert@example.com",
        string password = "NewPass456",
        string? confirmPassword = null) =>
        new(organizationName, kind, displayName, email, password, confirmPassword ?? password);

    private void AssertSignUpInvalid(SignUpResult result, string field)
    {
        Assert.Null(result.Response);
        Assert.Contains(field, result.Errors.Keys);
        Assert.Single(_users.Users);
        Assert.Single(_users.Organizations);
        Assert.Equal(0, _users.SaveCount);
    }

    private void AssertChangeInvalid(ChangePasswordResult result, string field)
    {
        Assert.Equal(ChangePasswordStatus.Invalid, result.Status);
        Assert.Contains(field, result.Errors.Keys);
        Assert.Equal("hashed:" + Password, _claire.PasswordHash);
        Assert.Equal(0, _users.SaveCount);
    }
}

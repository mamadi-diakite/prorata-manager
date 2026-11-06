namespace ProrataManager.Application.Contracts.Auth;

/// <summary>Sign-up creates a manager organization together with its first admin account.</summary>
/// <param name="OrganizationKind">"Contractor" or "ThirdParty".</param>
public sealed record SignUpRequest(
    string OrganizationName,
    string OrganizationKind,
    string DisplayName,
    string Email,
    string Password,
    string ConfirmPassword);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, CurrentUserResponse User);

/// <param name="Role">"ManagerAdmin" or "ManagerStaff".</param>
public sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, string Role, Guid OrganizationId);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

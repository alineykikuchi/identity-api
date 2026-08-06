namespace Identity.WebApi.Features.Users.ChangePassword;

/// <summary>
/// Request body for <c>PUT /api/me/password</c>.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>Current password of the authenticated user.</summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>New password. Must satisfy the password policy and differ from the current one.</summary>
    public string NewPassword { get; set; } = string.Empty;
}

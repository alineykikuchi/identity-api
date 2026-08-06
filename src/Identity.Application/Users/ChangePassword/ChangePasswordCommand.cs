using MediatR;

namespace Identity.Application.Users.ChangePassword;

/// <summary>
/// Command to replace the password of the authenticated user. Carries plaintext values;
/// they are verified and hashed by the handler and never persisted as-is.
/// </summary>
public class ChangePasswordCommand : IRequest
{
    /// <summary>Identifier of the authenticated user, taken from the access token claims.</summary>
    public Guid UserId { get; set; }

    /// <summary>Current password, checked against the stored hash before any change.</summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>New password. Must satisfy the password policy and differ from the current one.</summary>
    public string NewPassword { get; set; } = string.Empty;
}

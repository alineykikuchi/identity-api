using MediatR;

namespace Identity.Application.Auth.Logout;

/// <summary>
/// Command carrying the refresh token whose session should be terminated.
/// </summary>
public class LogoutCommand : IRequest
{
    /// <summary>The plaintext refresh token presented by the client.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

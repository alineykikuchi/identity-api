namespace Identity.WebApi.Features.Auth.Logout;

/// <summary>
/// Request body for <c>POST /api/logout</c>.
/// </summary>
public class LogoutRequest
{
    /// <summary>The refresh token of the session to terminate.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

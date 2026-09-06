namespace Identity.WebApi.Features.Auth.Login;

/// <summary>
/// Request body for <c>POST /api/login</c>.
/// </summary>
public class LoginRequest
{
    /// <summary>Login email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Plaintext password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional client id of the application signing in (for example <c>developer-store</c>).
    /// With it the access token carries that application's audience and the user's roles in
    /// it; without it the token is issued with neither, as before roles existed.
    /// </summary>
    public string? ClientId { get; set; }
}

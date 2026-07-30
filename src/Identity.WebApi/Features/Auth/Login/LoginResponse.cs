namespace Identity.WebApi.Features.Auth.Login;

/// <summary>
/// Response body for a successful login.
/// </summary>
public class LoginResponse
{
    /// <summary>Signed JWT access token.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Opaque refresh token (store securely; it is returned only once).</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Access-token lifetime, in seconds.</summary>
    public int ExpiresIn { get; set; }
}

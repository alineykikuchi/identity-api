namespace Identity.Application.Auth.Login;

/// <summary>
/// Tokens issued on a successful login. The refresh token is returned in plaintext here
/// only; the database stores just its hash.
/// </summary>
public class LoginResult
{
    /// <summary>Signed JWT access token.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Opaque refresh token (plaintext, returned once).</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Access-token lifetime, in seconds.</summary>
    public int ExpiresIn { get; set; }
}

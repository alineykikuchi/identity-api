namespace Identity.Application.Auth.RefreshToken;

/// <summary>
/// The rotated token pair returned by a successful refresh. The refresh token is
/// returned in plaintext here only; the database stores just its hash.
/// </summary>
public class RefreshTokenResult
{
    /// <summary>Newly issued JWT access token.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Newly issued refresh token (plaintext, returned once).</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Access-token lifetime, in seconds.</summary>
    public int ExpiresIn { get; set; }
}

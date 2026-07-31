namespace Identity.WebApi.Features.Auth.RefreshToken;

/// <summary>
/// Response body for a successful refresh: the rotated token pair.
/// </summary>
public class RefreshTokenResponse
{
    /// <summary>Newly issued JWT access token.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Newly issued refresh token (store securely; returned only once).</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Access-token lifetime, in seconds.</summary>
    public int ExpiresIn { get; set; }
}

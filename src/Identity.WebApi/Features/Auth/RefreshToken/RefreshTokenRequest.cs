namespace Identity.WebApi.Features.Auth.RefreshToken;

/// <summary>
/// Request body for <c>POST /api/refresh</c>.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>The refresh token previously issued to the client.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

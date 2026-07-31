using MediatR;

namespace Identity.Application.Auth.RefreshToken;

/// <summary>
/// Command carrying a refresh token to be exchanged for a new token pair.
/// </summary>
public class RefreshTokenCommand : IRequest<RefreshTokenResult>
{
    /// <summary>The plaintext refresh token presented by the client.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

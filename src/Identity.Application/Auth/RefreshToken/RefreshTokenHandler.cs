using Identity.Common.Security;
using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.Auth.RefreshToken;

/// <summary>
/// Exchanges a valid refresh token for a new access + refresh token pair (rotation).
/// </summary>
/// <remarks>
/// Every failure returns the same generic error. Rotation revokes the presented token and
/// issues a new one; if an already-revoked token is presented again — a sign the token was
/// stolen and replayed — every refresh token for that user is revoked.
/// </remarks>
public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private const string InvalidToken = "Invalid refresh token.";

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IConfiguration _configuration;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IConfiguration configuration)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _configuration = configuration;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenGenerator.Hash(command.RefreshToken);
        var stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is null)
        {
            throw new UnauthorizedAccessException(InvalidToken);
        }

        if (stored.RevokedAt is not null)
        {
            // A revoked token presented again signals possible theft: drop every session.
            await _refreshTokenRepository.RevokeAllByUserAsync(stored.UserId, cancellationToken);
            throw new UnauthorizedAccessException(InvalidToken);
        }

        if (!stored.IsActive)
        {
            // Not revoked, but expired.
            throw new UnauthorizedAccessException(InvalidToken);
        }

        var user = await _userRepository.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException(InvalidToken);
        }

        // Rotate: revoke the presented token and issue a fresh pair.
        await _refreshTokenRepository.RevokeAsync(stored, cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var accessTokenMinutes = JwtSettings.GetAccessTokenMinutes(_configuration);
        var refreshTokenDays = JwtSettings.GetRefreshTokenDays(_configuration);

        var rawRefreshToken = _refreshTokenGenerator.Generate();
        var newRefreshToken = new RefreshTokenEntity(
            user.Id,
            _refreshTokenGenerator.Hash(rawRefreshToken),
            DateTime.UtcNow.AddDays(refreshTokenDays));

        await _refreshTokenRepository.CreateAsync(newRefreshToken, cancellationToken);

        return new RefreshTokenResult
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresIn = accessTokenMinutes * 60
        };
    }
}

using Identity.Common.Security;
using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using ApplicationEntity = Identity.Domain.Entities.Application;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.Auth.RefreshToken;

/// <summary>
/// Exchanges a valid refresh token for a new access + refresh token pair (rotation).
/// </summary>
/// <remarks>
/// Every failure returns the same generic error. Rotation revokes the presented token and
/// issues a new one; if an already-revoked token is presented again — a sign the token was
/// stolen and replayed — every refresh token for that user is revoked.
///
/// A token issued for an application keeps that scope across rotations: the new access token
/// carries the same audience, and the application is carried over to the new refresh token.
/// Roles are read again on every refresh, so a role granted or removed since login takes
/// effect on the next rotation rather than being frozen at login time.
/// </remarks>
public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private const string InvalidToken = "Invalid refresh token.";

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IConfiguration _configuration;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IApplicationRepository applicationRepository,
        IRoleRepository roleRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IConfiguration configuration)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _applicationRepository = applicationRepository;
        _roleRepository = roleRepository;
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

        var (application, roles) = await ResolveApplicationAsync(stored.ApplicationId, user.Id, cancellationToken);

        // Rotate: revoke the presented token and issue a fresh pair.
        await _refreshTokenRepository.RevokeAsync(stored, cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateToken(user, application?.ClientId, roles);
        var accessTokenMinutes = JwtSettings.GetAccessTokenMinutes(_configuration);
        var refreshTokenDays = JwtSettings.GetRefreshTokenDays(_configuration);

        var rawRefreshToken = _refreshTokenGenerator.Generate();
        var newRefreshToken = new RefreshTokenEntity(
            user.Id,
            _refreshTokenGenerator.Hash(rawRefreshToken),
            DateTime.UtcNow.AddDays(refreshTokenDays),
            stored.ApplicationId);

        await _refreshTokenRepository.CreateAsync(newRefreshToken, cancellationToken);

        return new RefreshTokenResult
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresIn = accessTokenMinutes * 60
        };
    }

    /// <summary>
    /// Reloads the application the token is scoped to and the user's current roles in it.
    /// Returns neither when the token is not scoped to an application.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application has been removed or deactivated since the token was
    /// issued — refreshing must not keep minting tokens for an application that can no
    /// longer be used to log in.
    /// </exception>
    private async Task<(ApplicationEntity? Application, IReadOnlyCollection<string> Roles)> ResolveApplicationAsync(
        Guid? applicationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (applicationId is null)
        {
            return (null, Array.Empty<string>());
        }

        var application = await _applicationRepository.GetByIdAsync(applicationId.Value, cancellationToken);

        if (application is null || !application.IsActive)
        {
            throw new UnauthorizedAccessException(InvalidToken);
        }

        var applicationRoles = await _roleRepository.GetByUserAndApplicationAsync(
            userId,
            application.Id,
            cancellationToken);

        return (application, applicationRoles.Select(role => role.Name).ToArray());
    }
}

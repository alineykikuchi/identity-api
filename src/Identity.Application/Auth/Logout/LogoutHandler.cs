using Identity.Common.Security;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Auth.Logout;

/// <summary>
/// Terminates a session by revoking the refresh token presented by the client.
/// </summary>
/// <remarks>
/// The token is revoked rather than deleted: the record is kept for auditing and it keeps
/// feeding the reuse detection in the refresh use case. The operation is idempotent — an
/// unknown, already revoked or expired token is a no-op instead of an error, so logout
/// never fails for the client.
/// </remarks>
public class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;

    public LogoutHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenGenerator refreshTokenGenerator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenGenerator = refreshTokenGenerator;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenGenerator.Hash(command.RefreshToken);
        var stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is null || stored.RevokedAt is not null)
        {
            // Nothing to revoke: unknown token or a session that is already closed.
            return;
        }

        await _refreshTokenRepository.RevokeAsync(stored, cancellationToken);
    }
}

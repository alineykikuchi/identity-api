using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

/// <summary>
/// Persistence port for <see cref="RefreshToken"/> records. Implemented by a driven adapter.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>Persists a new refresh token and returns it.</summary>
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Gets a refresh token by its SHA-256 hash, or <c>null</c> if not found.</summary>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Revokes a single refresh token.</summary>
    Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes every active refresh token belonging to the given user.</summary>
    Task RevokeAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

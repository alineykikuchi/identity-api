namespace Identity.Domain.Entities;

/// <summary>
/// A refresh token issued to a user. Only the SHA-256 hash of the token is persisted
/// (see <see cref="TokenHash"/>) so a database leak does not expose usable tokens.
/// </summary>
public class RefreshToken
{
    /// <summary>Unique identifier of the refresh token record.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning user's identifier.</summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Application this token was issued for, or <c>null</c> when the login carried no
    /// client id. Persisting it lets the refresh flow reissue access tokens with the same
    /// audience and application roles as the original login.
    /// </summary>
    public Guid? ApplicationId { get; private set; }

    /// <summary>SHA-256 hash of the token. The plaintext token is never stored.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Expiration timestamp (UTC).</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Revocation timestamp (UTC); <c>null</c> while the token is still valid.</summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>True when the token has not been revoked and has not expired.</summary>
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    /// <summary>Parameterless constructor required by EF Core.</summary>
    private RefreshToken()
    {
    }

    /// <summary>Creates a refresh token record for the given user.</summary>
    /// <param name="userId">Owning user's identifier.</param>
    /// <param name="tokenHash">SHA-256 hash of the issued token.</param>
    /// <param name="expiresAt">Expiration timestamp (UTC).</param>
    /// <param name="applicationId">Application the token is scoped to, or <c>null</c> when none.</param>
    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt, Guid? applicationId = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        ApplicationId = applicationId;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Marks the token as revoked. Idempotent: keeps the first revocation time.</summary>
    public void Revoke()
    {
        RevokedAt ??= DateTime.UtcNow;
    }
}

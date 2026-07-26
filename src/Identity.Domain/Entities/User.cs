using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

/// <summary>
/// A user account managed by the Identity provider. The plaintext password is never
/// stored: only its BCrypt hash (see <see cref="PasswordHash"/>).
/// </summary>
public class User
{
    /// <summary>Unique identifier of the user.</summary>
    public Guid Id { get; private set; }

    /// <summary>Login email. Stored normalized (trimmed and lowercased) and is unique.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>BCrypt hash of the user's password. Never the plaintext.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Optional display name.</summary>
    public string? Name { get; private set; }

    /// <summary>Current lifecycle status. Only <see cref="UserStatus.Active"/> may authenticate.</summary>
    public UserStatus Status { get; private set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Last update timestamp (UTC).</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Parameterless constructor required by EF Core.</summary>
    private User()
    {
    }

    /// <summary>
    /// Creates a new active user. The email is normalized; the password must already be hashed.
    /// </summary>
    /// <param name="email">Login email; will be trimmed and lowercased.</param>
    /// <param name="passwordHash">BCrypt hash of the password.</param>
    /// <param name="name">Optional display name.</param>
    public User(string email, string passwordHash, string? name = null)
    {
        Id = Guid.NewGuid();
        Email = Normalize(email);
        PasswordHash = passwordHash;
        Name = name;
        Status = UserStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>Normalizes an email for storage and comparison (trim + lowercase).</summary>
    public static string Normalize(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}

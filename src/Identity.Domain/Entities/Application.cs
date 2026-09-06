namespace Identity.Domain.Entities;

/// <summary>
/// A consuming application registered with this Identity provider.
/// </summary>
/// <remarks>
/// Users are global — one account serves every application — but roles are scoped per
/// application, mirroring the Keycloak client / client-role model. The <see cref="ClientId"/>
/// is the public identifier a client sends at login and becomes the token's audience.
/// </remarks>
public class Application
{
    /// <summary>Unique identifier of the application.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Public client identifier (for example <c>developer-store</c>). Stored normalized
    /// (trimmed and lowercased) and is unique.
    /// </summary>
    public string ClientId { get; private set; } = string.Empty;

    /// <summary>Human-readable application name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Whether the application may still be used to authenticate.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Parameterless constructor required by EF Core.</summary>
    private Application()
    {
    }

    /// <summary>Registers a new active application.</summary>
    /// <param name="clientId">Public client identifier; will be trimmed and lowercased.</param>
    /// <param name="name">Human-readable application name.</param>
    public Application(string clientId, string name)
    {
        Id = Guid.NewGuid();
        ClientId = NormalizeClientId(clientId);
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Deactivates the application so it can no longer be used to authenticate.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>Normalizes a client id for storage and comparison (trim + lowercase).</summary>
    public static string NormalizeClientId(string clientId) =>
        (clientId ?? string.Empty).Trim().ToLowerInvariant();
}

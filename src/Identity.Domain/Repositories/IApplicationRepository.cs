using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

/// <summary>
/// Persistence port for <see cref="Application"/> records. Implemented by a driven adapter.
/// </summary>
public interface IApplicationRepository
{
    /// <summary>Persists a new application and returns it.</summary>
    Task<Application> CreateAsync(Application application, CancellationToken cancellationToken = default);

    /// <summary>Gets an application by its client id, or <c>null</c> if not found.</summary>
    Task<Application?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an application by id, or <c>null</c> if not found. Used by the refresh flow,
    /// which stores the application on the refresh token and needs its client id back to
    /// keep issuing tokens for the same audience.
    /// </summary>
    Task<Application?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

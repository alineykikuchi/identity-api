using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

/// <summary>
/// Persistence port for <see cref="User"/> aggregates. Implemented by a driven adapter.
/// </summary>
public interface IUserRepository
{
    /// <summary>Persists a new user and returns it.</summary>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Gets a user by id, or <c>null</c> if not found.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a user by email (matched against the normalized value), or <c>null</c> if not found.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing user and returns it.</summary>
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
}

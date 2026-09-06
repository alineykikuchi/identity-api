using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

/// <summary>
/// Persistence port for <see cref="Role"/> records and their assignment to users.
/// Implemented by a driven adapter.
/// </summary>
public interface IRoleRepository
{
    /// <summary>Persists a new role and returns it.</summary>
    Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the roles a user holds in one application. Returns an empty collection when the
    /// user holds none — a user with no roles can still authenticate.
    /// </summary>
    Task<IReadOnlyCollection<Role>> GetByUserAndApplicationAsync(
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken = default);

    /// <summary>Assigns a role to a user. Assigning an already held role is a no-op.</summary>
    Task AssignToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Removes a role from a user. Removing a role the user does not hold is a no-op.</summary>
    Task RemoveFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}

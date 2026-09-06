namespace Identity.Domain.Entities;

/// <summary>
/// Assignment of a global <see cref="User"/> to a <see cref="Role"/> of one application.
/// </summary>
/// <remarks>
/// The pair (<see cref="UserId"/>, <see cref="RoleId"/>) is the primary key, so the same
/// role cannot be assigned twice. Because a role already belongs to one application, this
/// is what scopes a user's permissions to a single application.
/// </remarks>
public class UserRole
{
    /// <summary>Identifier of the user holding the role.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Identifier of the assigned role.</summary>
    public Guid RoleId { get; private set; }

    /// <summary>Parameterless constructor required by EF Core.</summary>
    private UserRole()
    {
    }

    /// <summary>Assigns a role to a user.</summary>
    /// <param name="userId">Identifier of the user.</param>
    /// <param name="roleId">Identifier of the role.</param>
    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}

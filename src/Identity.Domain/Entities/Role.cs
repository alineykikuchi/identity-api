namespace Identity.Domain.Entities;

/// <summary>
/// A role belonging to a single <see cref="Application"/>.
/// </summary>
/// <remarks>
/// Roles are unique per (application, name), so "Admin" in one application is a different
/// role from "Admin" in another and carries no meaning outside its own application.
/// </remarks>
public class Role
{
    /// <summary>Unique identifier of the role.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identifier of the application this role belongs to.</summary>
    public Guid ApplicationId { get; private set; }

    /// <summary>Role name as it appears in the token's <c>role</c> claim.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Optional description of what the role grants.</summary>
    public string? Description { get; private set; }

    /// <summary>Parameterless constructor required by EF Core.</summary>
    private Role()
    {
    }

    /// <summary>Creates a role for the given application.</summary>
    /// <param name="applicationId">Owning application's identifier.</param>
    /// <param name="name">Role name; trimmed before being stored.</param>
    /// <param name="description">Optional description.</param>
    public Role(Guid applicationId, string name, string? description = null)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        Name = (name ?? string.Empty).Trim();
        Description = description;
    }
}

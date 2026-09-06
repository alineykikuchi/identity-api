using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.ORM.Repositories;

/// <summary>EF Core implementation of <see cref="IRoleRepository"/>.</summary>
public class RoleRepository : IRoleRepository
{
    private readonly DefaultContext _context;

    public RoleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task<IReadOnlyCollection<Role>> GetByUserAndApplicationAsync(
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        // Join through the assignments and filter by application, so roles the user holds
        // in other applications never reach this application's token.
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(
                _context.Roles.Where(r => r.ApplicationId == applicationId),
                ur => ur.RoleId,
                r => r.Id,
                (_, role) => role)
            .ToListAsync(cancellationToken);
    }

    public async Task AssignToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var alreadyAssigned = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (alreadyAssigned)
        {
            return;
        }

        await _context.UserRoles.AddAsync(new UserRole(userId, roleId), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (assignment is null)
        {
            return;
        }

        _context.UserRoles.Remove(assignment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

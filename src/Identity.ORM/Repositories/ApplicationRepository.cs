using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.ORM.Repositories;

/// <summary>EF Core implementation of <see cref="IApplicationRepository"/>.</summary>
public class ApplicationRepository : IApplicationRepository
{
    private readonly DefaultContext _context;

    public ApplicationRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Application> CreateAsync(Application application, CancellationToken cancellationToken = default)
    {
        await _context.Applications.AddAsync(application, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return application;
    }

    public async Task<Application?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var normalized = Application.NormalizeClientId(clientId);

        return await _context.Applications
            .FirstOrDefaultAsync(a => a.ClientId == normalized, cancellationToken);
    }

    public async Task<Application?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}

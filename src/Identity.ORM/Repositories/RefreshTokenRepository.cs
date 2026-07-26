using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.ORM.Repositories;

/// <summary>EF Core implementation of <see cref="IRefreshTokenRepository"/>.</summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DefaultContext _context;

    public RefreshTokenRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return refreshToken;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    public async Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        refreshToken.Revoke();
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

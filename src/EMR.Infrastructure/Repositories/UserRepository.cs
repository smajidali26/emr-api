using EMR.Application.Common.Interfaces;
using EMR.Domain.Entities;
using EMR.Domain.Interfaces;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMR.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context, ICurrentUserService currentUserService)
        : base(context, currentUserService)
    {
    }

    /// <summary>
    /// Find user by email address
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    /// <summary>
    /// Find user by Azure AD B2C identifier
    /// </summary>
    public async Task<User?> GetByAzureAdB2CIdAsync(string azureAdB2CId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(azureAdB2CId))
            throw new ArgumentException("Azure AD B2C ID cannot be empty", nameof(azureAdB2CId));

        var normalizedAzureId = azureAdB2CId.Trim();
        return await _dbSet.FirstOrDefaultAsync(u => u.AzureAdB2CId == normalizedAzureId, cancellationToken);
    }

    /// <summary>
    /// Check if email already exists in the system
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _dbSet.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    /// <summary>
    /// Check if Azure AD B2C ID already exists in the system
    /// </summary>
    public async Task<bool> AzureAdB2CIdExistsAsync(string azureAdB2CId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(azureAdB2CId))
            return false;

        var normalizedAzureId = azureAdB2CId.Trim();
        return await _dbSet.AnyAsync(u => u.AzureAdB2CId == normalizedAzureId, cancellationToken);
    }

    /// <summary>
    /// Get all active users
    /// </summary>
    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(u => u.IsActive).ToListAsync(cancellationToken);
    }
}

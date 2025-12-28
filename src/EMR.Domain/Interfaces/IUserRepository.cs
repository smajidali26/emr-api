using EMR.Domain.Entities;

namespace EMR.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Find user by email address
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find user by Azure AD B2C identifier
    /// </summary>
    Task<User?> GetByAzureAdB2CIdAsync(string azureAdB2CId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if email already exists in the system
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if Azure AD B2C ID already exists in the system
    /// </summary>
    Task<bool> AzureAdB2CIdExistsAsync(string azureAdB2CId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active users
    /// </summary>
    Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
}

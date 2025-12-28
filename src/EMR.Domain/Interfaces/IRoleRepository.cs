using EMR.Domain.Entities;
using EMR.Domain.Enums;

namespace EMR.Domain.Interfaces;

/// <summary>
/// Repository interface for Role entity operations
/// </summary>
public interface IRoleRepository : IRepository<Role>
{
    /// <summary>
    /// Gets a role by its name
    /// </summary>
    Task<Role?> GetByRoleNameAsync(UserRole roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all system-defined roles
    /// </summary>
    Task<IEnumerable<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role with its permissions
    /// </summary>
    Task<Role?> GetWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a specific role
    /// </summary>
    Task<IEnumerable<Permission>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role has a specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(Guid roleId, Permission permission, CancellationToken cancellationToken = default);
}

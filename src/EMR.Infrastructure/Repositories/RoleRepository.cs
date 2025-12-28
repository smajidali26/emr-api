using EMR.Application.Common.Interfaces;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Domain.Interfaces;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMR.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Role entity
/// </summary>
public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext context, ICurrentUserService currentUserService)
        : base(context, currentUserService)
    {
    }

    public async Task<Role?> GetByRoleNameAsync(UserRole roleName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.RoleName == roleName && !r.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Permissions)
            .Where(r => r.IsSystemRole && !r.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await GetWithPermissionsAsync(roleId, cancellationToken);
        return role?.GetActivePermissions() ?? Enumerable.Empty<Permission>();
    }

    public async Task<bool> HasPermissionAsync(Guid roleId, Permission permission, CancellationToken cancellationToken = default)
    {
        var role = await GetByIdAsync(roleId, cancellationToken);
        return role?.HasPermission(permission) ?? false;
    }
}

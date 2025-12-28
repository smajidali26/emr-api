using EMR.Domain.Common;
using EMR.Domain.Enums;

namespace EMR.Domain.Entities;

/// <summary>
/// Maps permissions to roles in the RBAC system
/// This is a many-to-many relationship entity
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>
    /// Role identifier
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Permission assigned to the role
    /// </summary>
    public Permission Permission { get; private set; }

    /// <summary>
    /// Navigation property to the role
    /// </summary>
    public Role? Role { get; private set; }

    // Private constructor for EF Core
    private RolePermission() { }

    /// <summary>
    /// Creates a new role-permission mapping
    /// </summary>
    public RolePermission(Guid roleId, Permission permission, string grantedBy)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role ID cannot be empty", nameof(roleId));

        RoleId = roleId;
        Permission = permission;
        CreatedBy = grantedBy;
    }
}

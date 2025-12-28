using EMR.Domain.Common;
using EMR.Domain.Enums;

namespace EMR.Domain.Entities;

/// <summary>
/// Represents a role in the RBAC system
/// Roles are collections of permissions that can be assigned to users
/// </summary>
public class Role : BaseEntity
{
    private readonly List<RolePermission> _permissions = new();

    /// <summary>
    /// Role name (Admin, Doctor, Nurse, Staff, Patient)
    /// Maps to UserRole enum for backward compatibility
    /// </summary>
    public UserRole RoleName { get; private set; }

    /// <summary>
    /// Display name of the role
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Description of the role's purpose and capabilities
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Indicates whether this is a system-defined role (cannot be modified)
    /// </summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>
    /// Permissions assigned to this role
    /// </summary>
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    // Private constructor for EF Core
    private Role() { }

    /// <summary>
    /// Creates a new role instance
    /// </summary>
    public Role(
        UserRole roleName,
        string displayName,
        string description,
        bool isSystemRole,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        RoleName = roleName;
        DisplayName = displayName.Trim();
        Description = description.Trim();
        IsSystemRole = isSystemRole;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Updates role information
    /// </summary>
    public void Update(string displayName, string description, string updatedBy)
    {
        if (IsSystemRole)
            throw new InvalidOperationException("System roles cannot be modified");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        DisplayName = displayName.Trim();
        Description = description.Trim();
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Adds a permission to the role
    /// </summary>
    public void AddPermission(Permission permission, string grantedBy)
    {
        if (IsSystemRole)
            throw new InvalidOperationException("System role permissions cannot be modified directly");

        if (_permissions.Any(p => p.Permission == permission && !p.IsDeleted))
            return; // Permission already exists

        var rolePermission = new RolePermission(Id, permission, grantedBy);
        _permissions.Add(rolePermission);
        MarkAsUpdated(grantedBy);
    }

    /// <summary>
    /// Removes a permission from the role
    /// </summary>
    public void RemovePermission(Permission permission, string removedBy)
    {
        if (IsSystemRole)
            throw new InvalidOperationException("System role permissions cannot be modified directly");

        var rolePermission = _permissions.FirstOrDefault(p => p.Permission == permission && !p.IsDeleted);
        if (rolePermission != null)
        {
            rolePermission.MarkAsDeleted(removedBy);
            MarkAsUpdated(removedBy);
        }
    }

    /// <summary>
    /// Checks if the role has a specific permission
    /// </summary>
    public bool HasPermission(Permission permission)
    {
        return _permissions.Any(p => p.Permission == permission && !p.IsDeleted);
    }

    /// <summary>
    /// Gets all active permissions for the role
    /// </summary>
    public IEnumerable<Permission> GetActivePermissions()
    {
        return _permissions.Where(p => !p.IsDeleted).Select(p => p.Permission);
    }
}

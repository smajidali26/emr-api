using EMR.Domain.Enums;

namespace EMR.Application.Features.Roles.DTOs;

/// <summary>
/// Data transfer object for Role
/// </summary>
public class RoleDto
{
    public Guid Id { get; set; }
    public UserRole RoleName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public IEnumerable<Permission> Permissions { get; set; } = Enumerable.Empty<Permission>();
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

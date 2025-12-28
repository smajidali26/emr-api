using EMR.Domain.Enums;

namespace EMR.Application.Features.Roles.DTOs;

/// <summary>
/// Data transfer object for Permission information
/// </summary>
public class PermissionDto
{
    public Permission Permission { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

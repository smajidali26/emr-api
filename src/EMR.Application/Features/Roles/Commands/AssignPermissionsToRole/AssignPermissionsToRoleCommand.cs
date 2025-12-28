using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Domain.Enums;

namespace EMR.Application.Features.Roles.Commands.AssignPermissionsToRole;

/// <summary>
/// Command to assign permissions to a role
/// </summary>
public record AssignPermissionsToRoleCommand : ICommand<ResultDto<bool>>
{
    public Guid RoleId { get; init; }
    public IEnumerable<Permission> Permissions { get; init; } = Enumerable.Empty<Permission>();
}

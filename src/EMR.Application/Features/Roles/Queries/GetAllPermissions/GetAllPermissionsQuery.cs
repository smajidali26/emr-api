using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Roles.DTOs;

namespace EMR.Application.Features.Roles.Queries.GetAllPermissions;

/// <summary>
/// Query to get all available permissions in the system
/// </summary>
public record GetAllPermissionsQuery : IQuery<ResultDto<IEnumerable<PermissionDto>>>;

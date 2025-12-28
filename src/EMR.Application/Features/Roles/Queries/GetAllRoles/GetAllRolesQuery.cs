using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Roles.DTOs;

namespace EMR.Application.Features.Roles.Queries.GetAllRoles;

/// <summary>
/// Query to get all roles in the system
/// </summary>
public record GetAllRolesQuery : IQuery<ResultDto<IEnumerable<RoleDto>>>;

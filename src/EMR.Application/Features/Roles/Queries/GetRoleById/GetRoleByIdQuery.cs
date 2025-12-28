using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Roles.DTOs;

namespace EMR.Application.Features.Roles.Queries.GetRoleById;

/// <summary>
/// Query to get a role by its ID
/// </summary>
public record GetRoleByIdQuery(Guid RoleId) : IQuery<ResultDto<RoleDto>>;

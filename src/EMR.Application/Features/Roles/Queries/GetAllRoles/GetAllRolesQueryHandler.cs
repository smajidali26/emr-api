using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Roles.DTOs;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Roles.Queries.GetAllRoles;

/// <summary>
/// Handler for GetAllRolesQuery
/// </summary>
public class GetAllRolesQueryHandler : IQueryHandler<GetAllRolesQuery, ResultDto<IEnumerable<RoleDto>>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<GetAllRolesQueryHandler> _logger;

    public GetAllRolesQueryHandler(
        IRoleRepository roleRepository,
        ILogger<GetAllRolesQueryHandler> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<ResultDto<IEnumerable<RoleDto>>> Handle(
        GetAllRolesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching all roles");

            var roles = await _roleRepository.GetAllAsync(cancellationToken);

            var roleDtos = roles.Select(role => new RoleDto
            {
                Id = role.Id,
                RoleName = role.RoleName,
                DisplayName = role.DisplayName,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                Permissions = role.GetActivePermissions(),
                CreatedAt = role.CreatedAt,
                CreatedBy = role.CreatedBy,
                UpdatedAt = role.UpdatedAt,
                UpdatedBy = role.UpdatedBy
            }).ToList();

            _logger.LogInformation("Successfully fetched {Count} roles", roleDtos.Count);

            return ResultDto<IEnumerable<RoleDto>>.Success(roleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all roles");
            return ResultDto<IEnumerable<RoleDto>>.Failure("Failed to fetch roles");
        }
    }
}

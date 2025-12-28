using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Roles.DTOs;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Roles.Queries.GetRoleById;

/// <summary>
/// Handler for GetRoleByIdQuery
/// </summary>
public class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, ResultDto<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<GetRoleByIdQueryHandler> _logger;

    public GetRoleByIdQueryHandler(
        IRoleRepository roleRepository,
        ILogger<GetRoleByIdQueryHandler> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<ResultDto<RoleDto>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching role with ID: {RoleId}", request.RoleId);

            var role = await _roleRepository.GetWithPermissionsAsync(request.RoleId, cancellationToken);

            if (role == null)
            {
                _logger.LogWarning("Role not found with ID: {RoleId}", request.RoleId);
                return ResultDto<RoleDto>.Failure("Role not found");
            }

            var roleDto = new RoleDto
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
            };

            _logger.LogInformation("Successfully fetched role: {RoleName}", role.DisplayName);

            return ResultDto<RoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching role with ID: {RoleId}", request.RoleId);
            return ResultDto<RoleDto>.Failure("Failed to fetch role");
        }
    }
}

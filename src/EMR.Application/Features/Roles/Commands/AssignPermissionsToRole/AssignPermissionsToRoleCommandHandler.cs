using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Roles.Commands.AssignPermissionsToRole;

/// <summary>
/// Handler for AssignPermissionsToRoleCommand
/// </summary>
public class AssignPermissionsToRoleCommandHandler : ICommandHandler<AssignPermissionsToRoleCommand, ResultDto<bool>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AssignPermissionsToRoleCommandHandler> _logger;

    public AssignPermissionsToRoleCommandHandler(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<AssignPermissionsToRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ResultDto<bool>> Handle(
        AssignPermissionsToRoleCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.GetUserId()?.ToString() ?? "System";
            _logger.LogInformation(
                "Assigning {Count} permissions to role {RoleId} by user {UserId}",
                request.Permissions.Count(),
                request.RoleId,
                currentUserId);

            var role = await _roleRepository.GetWithPermissionsAsync(request.RoleId, cancellationToken);

            if (role == null)
            {
                _logger.LogWarning("Role not found: {RoleId}", request.RoleId);
                return ResultDto<bool>.Failure("Role not found");
            }

            if (role.IsSystemRole)
            {
                _logger.LogWarning("Attempt to modify system role: {RoleName}", role.RoleName);
                return ResultDto<bool>.Failure("System roles cannot be modified");
            }

            // Get current permissions
            var currentPermissions = role.GetActivePermissions().ToHashSet();
            var newPermissions = request.Permissions.ToHashSet();

            // Remove permissions that are no longer needed
            var permissionsToRemove = currentPermissions.Except(newPermissions);
            foreach (var permission in permissionsToRemove)
            {
                role.RemovePermission(permission, currentUserId);
            }

            // Add new permissions
            var permissionsToAdd = newPermissions.Except(currentPermissions);
            foreach (var permission in permissionsToAdd)
            {
                role.AddPermission(permission, currentUserId);
            }

            _roleRepository.Update(role);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully assigned permissions to role {RoleName}. Added: {Added}, Removed: {Removed}",
                role.DisplayName,
                permissionsToAdd.Count(),
                permissionsToRemove.Count());

            return ResultDto<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permissions to role {RoleId}", request.RoleId);
            return ResultDto<bool>.Failure("Failed to assign permissions to role");
        }
    }
}

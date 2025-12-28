using EMR.Api.Attributes;
using EMR.Application.Features.Roles.Commands.AssignPermissionsToRole;
using EMR.Application.Features.Roles.DTOs;
using EMR.Application.Features.Roles.Queries.GetAllPermissions;
using EMR.Application.Features.Roles.Queries.GetAllRoles;
using EMR.Application.Features.Roles.Queries.GetRoleById;
using EMR.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMR.Api.Controllers;

/// <summary>
/// Role and permission management controller
/// Provides endpoints for managing roles and permissions (Admin only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IMediator mediator, ILogger<RolesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles in the system
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all roles with their permissions</returns>
    [HttpGet]
    [HasPermission(Permission.RolesView)]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllRoles(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllRoles endpoint called");

        var query = new GetAllRolesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get all roles: {Error}", result.ErrorMessage);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a specific role by ID
    /// </summary>
    /// <param name="id">Role identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role details with permissions</returns>
    [HttpGet("{id}")]
    [HasPermission(Permission.RolesView)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetRoleById endpoint called for role: {RoleId}", id);

        var query = new GetRoleByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get role {RoleId}: {Error}", id, result.ErrorMessage);
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all available permissions in the system
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all permissions with descriptions</returns>
    [HttpGet("permissions")]
    [HasPermission(Permission.PermissionsView)]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllPermissions(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllPermissions endpoint called");

        var query = new GetAllPermissionsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get all permissions: {Error}", result.ErrorMessage);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Assign permissions to a role
    /// </summary>
    /// <param name="id">Role identifier</param>
    /// <param name="request">List of permissions to assign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPut("{id}/permissions")]
    [HasPermission(Permission.PermissionsAssign)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignPermissions(
        Guid id,
        [FromBody] AssignPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AssignPermissions endpoint called for role {RoleId} with {Count} permissions",
            id,
            request.Permissions.Count());

        var command = new AssignPermissionsToRoleCommand
        {
            RoleId = id,
            Permissions = request.Permissions
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to assign permissions to role {RoleId}: {Error}", id, result.ErrorMessage);

            if (result.ErrorMessage?.Contains("not found") == true)
            {
                return NotFound(new { message = result.ErrorMessage });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Permissions assigned successfully" });
    }
}

/// <summary>
/// Request model for assigning permissions to a role
/// </summary>
public class AssignPermissionsRequest
{
    /// <summary>
    /// List of permissions to assign to the role
    /// </summary>
    public IEnumerable<Permission> Permissions { get; set; } = Enumerable.Empty<Permission>();
}

using EMR.Application.Common.Interfaces;
using EMR.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using IAppAuthorizationService = EMR.Application.Common.Interfaces.IAuthorizationService;

namespace EMR.Infrastructure.Authorization;

/// <summary>
/// Authorization handler for permission-based policies
/// Integrates with ASP.NET Core's policy-based authorization
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IAppAuthorizationService _authorizationService;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IAppAuthorizationService authorizationService,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        try
        {
            var hasPermission = await _authorizationService.HasPermissionAsync(
                requirement.Permission,
                CancellationToken.None);

            if (hasPermission)
            {
                _logger.LogDebug(
                    "Authorization succeeded for permission {Permission}",
                    requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                var userId = _authorizationService.GetCurrentUserId();
                _logger.LogWarning(
                    "AUTHORIZATION_DENIED | UserId: {UserId} | Permission: {Permission} | Reason: InsufficientPermissions",
                    userId,
                    requirement.Permission);

                // SECURITY FIX: Explicitly fail the authorization context
                // This ensures the failure is propagated to the authorization middleware
                context.Fail(new AuthorizationFailureReason(
                    this,
                    $"User lacks required permission: {requirement.Permission}"));
            }
        }
        catch (Exception ex)
        {
            var userId = _authorizationService.GetCurrentUserId();
            _logger.LogError(
                ex,
                "AUTHORIZATION_ERROR | UserId: {UserId} | Permission: {Permission} | Error: {ErrorMessage}",
                userId,
                requirement.Permission,
                ex.Message);

            // SECURITY FIX: Fail closed on errors - deny access when authorization check fails
            context.Fail(new AuthorizationFailureReason(
                this,
                "Authorization check failed due to an internal error"));
        }
    }
}

/// <summary>
/// Authorization requirement for permission-based policies
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public Permission Permission { get; }

    public PermissionRequirement(Permission permission)
    {
        Permission = permission;
    }
}

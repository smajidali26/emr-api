using EMR.Application.Common.Interfaces;
using EMR.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using IAppAuthorizationService = EMR.Application.Common.Interfaces.IAuthorizationService;

namespace EMR.Infrastructure.Authorization;

/// <summary>
/// Authorization handler for resource-based (ABAC) authorization
/// Checks if user has permission on specific resource instances
/// </summary>
public class ResourceAuthorizationHandler : AuthorizationHandler<ResourcePermissionRequirement, ResourceAuthorizationContext>
{
    private readonly IAppAuthorizationService _authorizationService;
    private readonly ILogger<ResourceAuthorizationHandler> _logger;

    public ResourceAuthorizationHandler(
        IAppAuthorizationService authorizationService,
        ILogger<ResourceAuthorizationHandler> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourcePermissionRequirement requirement,
        ResourceAuthorizationContext resource)
    {
        try
        {
            var hasAccess = await _authorizationService.HasResourceAccessAsync(
                resource.ResourceType,
                resource.ResourceId,
                requirement.Permission,
                CancellationToken.None);

            if (hasAccess)
            {
                _logger.LogDebug(
                    "Resource authorization succeeded for {ResourceType}/{ResourceId} with permission {Permission}",
                    resource.ResourceType,
                    resource.ResourceId,
                    requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                var userId = _authorizationService.GetCurrentUserId();
                _logger.LogWarning(
                    "RESOURCE_ACCESS_DENIED | UserId: {UserId} | ResourceType: {ResourceType} | ResourceId: {ResourceId} | Permission: {Permission} | Reason: InsufficientAccess",
                    userId,
                    resource.ResourceType,
                    resource.ResourceId,
                    requirement.Permission);

                // SECURITY FIX: Explicitly fail the authorization context
                // This ensures the failure is propagated and properly logged
                context.Fail(new AuthorizationFailureReason(
                    this,
                    $"User lacks access to {resource.ResourceType}/{resource.ResourceId}"));
            }
        }
        catch (Exception ex)
        {
            var userId = _authorizationService.GetCurrentUserId();
            _logger.LogError(
                ex,
                "RESOURCE_ACCESS_ERROR | UserId: {UserId} | ResourceType: {ResourceType} | ResourceId: {ResourceId} | Error: {ErrorMessage}",
                userId,
                resource.ResourceType,
                resource.ResourceId,
                ex.Message);

            // SECURITY FIX: Fail closed on errors - deny access when authorization check fails
            context.Fail(new AuthorizationFailureReason(
                this,
                "Resource authorization check failed due to an internal error"));
        }
    }
}

/// <summary>
/// Authorization requirement for resource-based policies
/// </summary>
public class ResourcePermissionRequirement : IAuthorizationRequirement
{
    public Permission Permission { get; }

    public ResourcePermissionRequirement(Permission permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// Context object for resource authorization
/// Passed to the authorization handler to identify the resource being accessed
/// </summary>
public class ResourceAuthorizationContext
{
    public ResourceType ResourceType { get; set; }
    public Guid ResourceId { get; set; }

    public ResourceAuthorizationContext(ResourceType resourceType, Guid resourceId)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

using EMR.Application.Common.Interfaces;
using EMR.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EMR.Api.Filters;

/// <summary>
/// Action filter attribute for automatic PHI access logging
/// Apply this to controller actions that access Protected Health Information
/// Ensures HIPAA-compliant audit trail for all PHI access
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class PhiAccessLoggingAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Resource type being accessed (e.g., "Patient", "Encounter")
    /// </summary>
    public string ResourceType { get; set; } = "Unknown";

    /// <summary>
    /// Action description for audit log
    /// </summary>
    public string Action { get; set; } = "Accessed PHI";

    /// <summary>
    /// Route parameter name that contains the resource ID
    /// Default: "id"
    /// </summary>
    public string ResourceIdParameter { get; set; } = "id";

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var auditService = context.HttpContext.RequestServices
            .GetService<IAuditService>();

        var currentUserService = context.HttpContext.RequestServices
            .GetService<ICurrentUserService>();

        if (auditService == null || currentUserService == null)
        {
            await next();
            return;
        }

        var userId = currentUserService.GetUserId()?.ToString() ?? "Anonymous";
        var ipAddress = currentUserService.GetIpAddress();
        var userAgent = currentUserService.GetUserAgent();

        // Extract resource ID from route parameters
        string? resourceId = null;
        if (context.ActionArguments.ContainsKey(ResourceIdParameter))
        {
            resourceId = context.ActionArguments[ResourceIdParameter]?.ToString();
        }

        // If not found in action arguments, try route values
        if (string.IsNullOrEmpty(resourceId) &&
            context.RouteData.Values.ContainsKey(ResourceIdParameter))
        {
            resourceId = context.RouteData.Values[ResourceIdParameter]?.ToString();
        }

        try
        {
            // Execute the action
            var executedContext = await next();

            // Determine if the action was successful
            var success = executedContext.Exception == null &&
                         (executedContext.HttpContext.Response.StatusCode >= 200 &&
                          executedContext.HttpContext.Response.StatusCode < 300);

            // Log PHI access
            await auditService.LogPhiAccessAsync(
                userId: userId,
                resourceType: ResourceType,
                resourceId: resourceId ?? "N/A",
                action: Action,
                ipAddress: ipAddress,
                userAgent: userAgent,
                details: $"Controller: {context.Controller.GetType().Name}, Action: {context.ActionDescriptor.DisplayName}");

            // Additional logging for failed access attempts
            if (!success && executedContext.Exception != null)
            {
                await auditService.LogAccessDeniedAsync(
                    userId: userId,
                    resourceType: ResourceType,
                    resourceId: resourceId,
                    action: Action,
                    reason: executedContext.Exception.Message,
                    ipAddress: ipAddress,
                    userAgent: userAgent);
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't throw - audit logging should not break the application
            var logger = context.HttpContext.RequestServices
                .GetService<ILogger<PhiAccessLoggingAttribute>>();
            logger?.LogError(ex, "Failed to log PHI access for {ResourceType}/{ResourceId}",
                ResourceType, resourceId);

            // Re-throw the original exception
            throw;
        }
    }
}

/// <summary>
/// Specialized filter for Patient PHI access
/// </summary>
public class PatientAccessLoggingAttribute : PhiAccessLoggingAttribute
{
    public PatientAccessLoggingAttribute()
    {
        ResourceType = "Patient";
        Action = "Accessed patient record";
        ResourceIdParameter = "id";
    }
}

/// <summary>
/// Specialized filter for Encounter PHI access
/// </summary>
public class EncounterAccessLoggingAttribute : PhiAccessLoggingAttribute
{
    public EncounterAccessLoggingAttribute()
    {
        ResourceType = "Encounter";
        Action = "Accessed encounter record";
        ResourceIdParameter = "id";
    }
}

/// <summary>
/// Specialized filter for Medical Note PHI access
/// </summary>
public class MedicalNoteAccessLoggingAttribute : PhiAccessLoggingAttribute
{
    public MedicalNoteAccessLoggingAttribute()
    {
        ResourceType = "MedicalNote";
        Action = "Accessed medical note";
        ResourceIdParameter = "id";
    }
}

using EMR.Application.Common.Attributes;
using EMR.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace EMR.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behavior for automatic audit logging
/// Intercepts commands/queries decorated with [Auditable] attribute
/// and creates audit trail entries
/// </summary>
public class AuditLoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<AuditLoggingBehaviour<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public AuditLoggingBehaviour(
        ILogger<AuditLoggingBehaviour<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var auditableAttribute = typeof(TRequest).GetCustomAttribute<AuditableAttribute>();

        // If request is not marked as auditable, continue without logging
        if (auditableAttribute == null)
        {
            return await next();
        }

        var stopwatch = Stopwatch.StartNew();
        var userId = _currentUserService.GetUserId()?.ToString() ?? "Anonymous";
        var username = _currentUserService.GetUserEmail();
        var ipAddress = _currentUserService.GetIpAddress();
        var userAgent = _currentUserService.GetUserAgent();
        var success = true;
        string? errorMessage = null;
        string? resourceId = null;

        try
        {
            // Try to extract resource ID from request using reflection
            resourceId = ExtractResourceId(request);

            // Execute the request
            var response = await next();

            // If response contains a resource ID, extract it
            if (string.IsNullOrEmpty(resourceId))
            {
                resourceId = ExtractResourceId(response);
            }

            return response;
        }
        catch (Exception ex)
        {
            success = false;
            errorMessage = ex.Message;
            _logger.LogError(ex, "Error executing auditable request {RequestType}", typeof(TRequest).Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // SECURITY FIX: Task #3 - Fix fire-and-forget audit logging (Thomas Thompson - 16h)
            // CRITICAL: Replace fire-and-forget with proper async/await for guaranteed audit delivery
            // Audit logs are critical for HIPAA compliance and must not be silently lost
            try
            {
                // Use proper async/await instead of fire-and-forget Task.Run
                // This ensures audit logs are persisted before the request completes
                await _auditService.CreateAuditLogAsync(
                    eventType: auditableAttribute.EventType,
                    userId: userId,
                    action: auditableAttribute.Action,
                    resourceType: auditableAttribute.ResourceType,
                    resourceId: resourceId,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    success: success,
                    details: $"Request: {typeof(TRequest).Name}, Duration: {stopwatch.ElapsedMilliseconds}ms",
                    username: username,
                    cancellationToken: CancellationToken.None); // Use None to ensure audit completes even if request is cancelled

                // If this is a PHI access, log to Serilog as well for immediate SIEM shipping
                if (auditableAttribute.AccessesPhi && success)
                {
                    _logger.LogInformation(
                        "PHI_ACCESS | User: {UserId} | Resource: {ResourceType}/{ResourceId} | Action: {Action} | IP: {IpAddress} | Duration: {DurationMs}ms",
                        userId, auditableAttribute.ResourceType, resourceId ?? "N/A",
                        auditableAttribute.Action, ipAddress ?? "Unknown", stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                // CRITICAL: Log audit failure with high severity
                // In production, consider failing the request if audit logging fails
                // to ensure HIPAA compliance (all PHI access must be logged)
                _logger.LogError(ex,
                    "CRITICAL: Failed to create audit log for {RequestType}. " +
                    "This is a HIPAA compliance violation if PHI was accessed. " +
                    "EventType: {EventType}, ResourceType: {ResourceType}, ResourceId: {ResourceId}",
                    typeof(TRequest).Name, auditableAttribute.EventType,
                    auditableAttribute.ResourceType, resourceId ?? "N/A");

                // NOTE: Consider implementing Outbox pattern for even more reliable audit logging
                // Outbox pattern stores audit events in the same transaction as the business operation
                // and a background worker ensures they are eventually delivered to the audit log store
            }
        }
    }

    /// <summary>
    /// Extract resource ID from request or response using reflection
    /// Looks for common property names: Id, ResourceId, PatientId, EncounterId, etc.
    /// </summary>
    private string? ExtractResourceId(object obj)
    {
        if (obj == null)
            return null;

        var type = obj.GetType();

        // Try to find Id property
        var idProperty = type.GetProperty("Id")
            ?? type.GetProperty("ResourceId")
            ?? type.GetProperty("PatientId")
            ?? type.GetProperty("EncounterId")
            ?? type.GetProperty("UserId");

        if (idProperty != null)
        {
            var value = idProperty.GetValue(obj);
            return value?.ToString();
        }

        return null;
    }
}

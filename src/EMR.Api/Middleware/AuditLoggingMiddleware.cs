using EMR.Application.Common.Interfaces;
using EMR.Domain.Enums;
using System.Diagnostics;
using System.Security.Claims;

namespace EMR.Api.Middleware;

/// <summary>
/// Middleware for automatic HTTP request/response audit logging
/// Captures all API calls for HIPAA compliance and security monitoring
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        // Skip audit logging for certain endpoints
        if (ShouldSkipAudit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            // Execute the request
            await _next(context);

            stopwatch.Stop();

            // Log successful request
            await LogRequestAsync(
                context,
                auditService,
                currentUserService,
                stopwatch.ElapsedMilliseconds,
                success: true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log failed request
            await LogRequestAsync(
                context,
                auditService,
                currentUserService,
                stopwatch.ElapsedMilliseconds,
                success: false,
                errorMessage: ex.Message);

            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Log HTTP request to audit service
    /// </summary>
    private async Task LogRequestAsync(
        HttpContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        long durationMs,
        bool success,
        string? errorMessage = null)
    {
        try
        {
            var userId = currentUserService.GetUserId()?.ToString() ?? "Anonymous";
            var httpMethod = context.Request.Method;
            var requestPath = context.Request.Path.Value ?? "/";
            var statusCode = context.Response.StatusCode;
            var ipAddress = currentUserService.GetIpAddress();
            var userAgent = currentUserService.GetUserAgent();
            var sessionId = context.Session?.Id;
            var correlationId = context.TraceIdentifier;

            // Determine event type based on HTTP method
            var eventType = GetEventTypeFromHttpMethod(httpMethod);

            // Determine resource type from path
            var (resourceType, resourceId) = ExtractResourceFromPath(requestPath);

            // Create action description
            var action = $"{httpMethod} {requestPath}";

            // Only log to database for significant operations
            // (exclude health checks, OPTIONS, etc.)
            if (ShouldPersistAudit(httpMethod, requestPath, statusCode))
            {
                await auditService.LogHttpRequestAsync(
                    eventType: eventType,
                    userId: userId,
                    action: action,
                    resourceType: resourceType,
                    resourceId: resourceId,
                    httpMethod: httpMethod,
                    requestPath: requestPath,
                    statusCode: statusCode,
                    durationMs: durationMs,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    sessionId: sessionId,
                    correlationId: correlationId);
            }

            // Always log to Serilog for real-time SIEM shipping
            if (success)
            {
                _logger.LogInformation(
                    "HTTP_REQUEST | {HttpMethod} {RequestPath} | User: {UserId} | Status: {StatusCode} | " +
                    "Duration: {DurationMs}ms | IP: {IpAddress} | TraceId: {TraceId}",
                    httpMethod, requestPath, userId, statusCode, durationMs,
                    ipAddress ?? "Unknown", correlationId);
            }
            else
            {
                _logger.LogWarning(
                    "HTTP_REQUEST_FAILED | {HttpMethod} {RequestPath} | User: {UserId} | Status: {StatusCode} | " +
                    "Duration: {DurationMs}ms | IP: {IpAddress} | Error: {ErrorMessage} | TraceId: {TraceId}",
                    httpMethod, requestPath, userId, statusCode, durationMs,
                    ipAddress ?? "Unknown", errorMessage, correlationId);
            }
        }
        catch (Exception ex)
        {
            // Don't throw - audit logging failures should not break the application
            _logger.LogError(ex, "Failed to log HTTP request to audit service");
        }
    }

    /// <summary>
    /// Determine if the request should skip audit logging
    /// </summary>
    private static bool ShouldSkipAudit(PathString path)
    {
        var pathValue = path.Value?.ToLower() ?? string.Empty;

        return pathValue.Contains("/health") ||
               pathValue.Contains("/metrics") ||
               pathValue.Contains("/swagger") ||
               pathValue.Contains("/_framework") ||
               pathValue.Contains("/favicon.ico");
    }

    /// <summary>
    /// Determine if the audit should be persisted to database
    /// (vs. just logged to Serilog)
    /// </summary>
    private static bool ShouldPersistAudit(string httpMethod, string path, int statusCode)
    {
        // Don't persist OPTIONS requests (CORS preflight)
        if (httpMethod == "OPTIONS")
            return false;

        // Don't persist successful GET requests to non-PHI endpoints
        // (to reduce database storage for low-value audits)
        if (httpMethod == "GET" && statusCode >= 200 && statusCode < 300)
        {
            var pathLower = path.ToLower();
            // Skip generic list/search endpoints that don't access specific PHI
            if (pathLower.Contains("/api/audit") ||
                pathLower.Contains("/api/metadata") ||
                pathLower.Contains("/api/config"))
            {
                return false;
            }
        }

        // Persist all other requests
        return true;
    }

    /// <summary>
    /// Map HTTP method to audit event type
    /// </summary>
    private static AuditEventType GetEventTypeFromHttpMethod(string httpMethod)
    {
        return httpMethod.ToUpper() switch
        {
            "GET" => AuditEventType.View,
            "POST" => AuditEventType.Create,
            "PUT" => AuditEventType.Update,
            "PATCH" => AuditEventType.Update,
            "DELETE" => AuditEventType.Delete,
            _ => AuditEventType.View
        };
    }

    /// <summary>
    /// Extract resource type and ID from request path
    /// Example: /api/patients/123 => (Patient, 123)
    /// </summary>
    private static (string ResourceType, string? ResourceId) ExtractResourceFromPath(string path)
    {
        try
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2)
                return ("Api", null);

            // Skip 'api' segment if present
            var startIndex = segments[0].Equals("api", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            if (segments.Length <= startIndex)
                return ("Api", null);

            var resourceType = segments[startIndex];

            // Capitalize first letter
            if (!string.IsNullOrEmpty(resourceType))
            {
                resourceType = char.ToUpper(resourceType[0]) + resourceType.Substring(1);
            }

            // Try to extract resource ID (usually the segment after resource type)
            string? resourceId = null;
            if (segments.Length > startIndex + 1)
            {
                var potentialId = segments[startIndex + 1];
                // Check if it looks like an ID (GUID, number, or not a known action)
                if (Guid.TryParse(potentialId, out _) ||
                    int.TryParse(potentialId, out _) ||
                    !IsKnownAction(potentialId))
                {
                    resourceId = potentialId;
                }
            }

            return (resourceType, resourceId);
        }
        catch
        {
            return ("Api", null);
        }
    }

    /// <summary>
    /// Check if segment is a known API action (not a resource ID)
    /// </summary>
    private static bool IsKnownAction(string segment)
    {
        var knownActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "search", "export", "import", "validate", "verify",
            "approve", "reject", "cancel", "archive", "restore"
        };

        return knownActions.Contains(segment);
    }
}

/// <summary>
/// Extension methods for registering audit middleware
/// </summary>
public static class AuditLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLoggingMiddleware>();
    }
}

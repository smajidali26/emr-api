using EMR.Application.Common.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace EMR.Infrastructure.Authorization;

/// <summary>
/// Middleware to audit all authorization decisions
/// Logs successful and failed authorization attempts for security monitoring
/// </summary>
public class AuthorizationAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationAuditMiddleware> _logger;

    public AuthorizationAuditMiddleware(
        RequestDelegate next,
        ILogger<AuthorizationAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogger auditLogger)
    {
        // Record the start time
        var startTime = DateTime.UtcNow;

        try
        {
            // Continue processing the request
            await _next(context);

            // After the request completes, check for authorization results
            await LogAuthorizationResultAsync(context, auditLogger, startTime, wasSuccessful: context.Response.StatusCode != StatusCodes.Status401Unauthorized && context.Response.StatusCode != StatusCodes.Status403Forbidden);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            await LogAuthorizationResultAsync(context, auditLogger, startTime, wasSuccessful: false);
            throw;
        }
    }

    private async Task LogAuthorizationResultAsync(
        HttpContext context,
        IAuditLogger auditLogger,
        DateTime startTime,
        bool wasSuccessful)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.FindFirst("sub")?.Value
                     ?? context.User.FindFirst("userId")?.Value
                     ?? "Anonymous";

        var roles = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var endpoint = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "Unknown";

        var auditData = new
        {
            UserId = userId,
            Roles = roles,
            Endpoint = endpoint,
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            StatusCode = context.Response.StatusCode,
            WasSuccessful = wasSuccessful,
            Duration = (DateTime.UtcNow - startTime).TotalMilliseconds,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers["User-Agent"].ToString()
        };

        if (!wasSuccessful)
        {
            _logger.LogWarning(
                "Authorization failed for user {UserId} on {Method} {Path} - Status: {StatusCode}",
                userId,
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode);
        }

        // Log to audit trail
        await auditLogger.LogDataAccessAsync(
            userId: userId,
            action: wasSuccessful ? "AccessGranted" : "AccessDenied",
            resourceType: "Authorization",
            resourceId: endpoint,
            ipAddress: context.Connection.RemoteIpAddress?.ToString(),
            details: System.Text.Json.JsonSerializer.Serialize(auditData));
    }
}

/// <summary>
/// Extension method to add the authorization audit middleware to the pipeline
/// </summary>
public static class AuthorizationAuditMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthorizationAuditing(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizationAuditMiddleware>();
    }
}

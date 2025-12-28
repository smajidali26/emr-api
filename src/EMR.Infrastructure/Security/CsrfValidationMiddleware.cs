using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Security;

/// <summary>
/// Middleware to validate CSRF tokens for state-changing HTTP requests
/// SECURITY: Protects against Cross-Site Request Forgery attacks
/// </summary>
public class CsrfValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CsrfValidationMiddleware> _logger;
    private static readonly HashSet<string> StateChangingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    // Endpoints that don't require CSRF validation (login, token refresh, etc.)
    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/refresh",
        "/api/auth/logout",
        "/health"
    };

    public CsrfValidationMiddleware(
        RequestDelegate next,
        ILogger<CsrfValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Skip validation for non-state-changing requests
        if (!StateChangingMethods.Contains(method))
        {
            await _next(context);
            return;
        }

        // Skip validation for exempt paths
        if (ExemptPaths.Any(ep => path.StartsWith(ep, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Skip if request doesn't have authentication (public endpoints)
        // Use != true for clarity: only validate CSRF for authenticated users
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        try
        {
            // Validate the CSRF token
            await antiforgery.ValidateRequestAsync(context);
            await _next(context);
        }
        catch (AntiforgeryValidationException ex)
        {
            _logger.LogWarning(
                "CSRF_VALIDATION_FAILED | Path: {Path} | Method: {Method} | User: {UserId} | Error: {Error}",
                path,
                method,
                context.User.Identity?.Name ?? "unknown",
                ex.Message);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "CSRF validation failed",
                message = "Invalid or missing CSRF token"
            });
        }
    }
}

/// <summary>
/// Extension method to add the CSRF validation middleware to the pipeline
/// </summary>
public static class CsrfValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseCsrfValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CsrfValidationMiddleware>();
    }
}

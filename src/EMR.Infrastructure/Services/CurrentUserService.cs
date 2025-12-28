using EMR.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EMR.Infrastructure.Services;

/// <summary>
/// Service for accessing current user context from HTTP request
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.User.FindFirst("sub")?.Value
                          ?? httpContext.User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }

    public string? GetAzureAdB2CId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        return httpContext.User.FindFirst("oid")?.Value
               ?? httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
    }

    public string? GetUserEmail()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        return httpContext.User.FindFirst(ClaimTypes.Email)?.Value
               ?? httpContext.User.FindFirst("email")?.Value
               ?? httpContext.User.FindFirst("emails")?.Value;
    }

    public string? GetIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // Use the Connection.RemoteIpAddress which is properly set by the
        // ForwardedHeaders middleware when configured with trusted proxies.
        // This prevents IP spoofing as the middleware only processes X-Forwarded-For
        // headers from known proxy addresses configured in ForwardedHeadersOptions.
        //
        // SECURITY: Do NOT manually parse X-Forwarded-For headers here.
        // Configure UseForwardedHeaders() in Program.cs with proper KnownProxies/KnownNetworks.
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    public string? GetUserAgent()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        return httpContext.Request.Headers["User-Agent"].FirstOrDefault();
    }

    public bool IsAuthenticated()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.Identity?.IsAuthenticated == true;
    }
}

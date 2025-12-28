using EMR.Domain.Enums;
using System.Security.Claims;

namespace EMR.Api.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to simplify working with user claims
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from claims
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? principal.FindFirst("sub")?.Value
                          ?? principal.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }

    /// <summary>
    /// Gets the Azure AD B2C object identifier from claims
    /// </summary>
    public static string? GetAzureAdB2CId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("oid")?.Value
               ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
    }

    /// <summary>
    /// Gets the user's email from claims
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
               ?? principal.FindFirst("email")?.Value
               ?? principal.FindFirst("emails")?.Value;
    }

    /// <summary>
    /// Gets all user roles from claims
    /// </summary>
    public static IEnumerable<UserRole> GetRoles(this ClaimsPrincipal principal)
    {
        var roleClaims = principal.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        foreach (var roleClaim in roleClaims)
        {
            if (Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role))
            {
                yield return role;
            }
        }
    }

    /// <summary>
    /// Checks if the user has a specific role
    /// </summary>
    public static bool HasRole(this ClaimsPrincipal principal, UserRole role)
    {
        return principal.IsInRole(role.ToString());
    }

    /// <summary>
    /// Checks if the user has any of the specified roles
    /// </summary>
    public static bool HasAnyRole(this ClaimsPrincipal principal, params UserRole[] roles)
    {
        return roles.Any(role => principal.IsInRole(role.ToString()));
    }
}

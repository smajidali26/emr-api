using EMR.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace EMR.Api.Attributes;

/// <summary>
/// Custom authorization attribute for role-based access control
/// Use this attribute to protect endpoints with specific roles
/// </summary>
/// <example>
/// [RequireRole(UserRole.Admin, UserRole.Doctor)]
/// public async Task<IActionResult> GetSensitiveData() { ... }
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRoleAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Creates an authorization attribute that requires one or more roles
    /// </summary>
    /// <param name="roles">Required roles (user must have at least one)</param>
    public RequireRoleAttribute(params UserRole[] roles)
    {
        Roles = string.Join(",", roles.Select(r => r.ToString()));
    }
}

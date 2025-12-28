using EMR.Application.Common.Authorization;
using EMR.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace EMR.Api.Attributes;

/// <summary>
/// Custom authorization attribute for permission-based access control
/// Use this attribute to protect endpoints with specific permissions
/// </summary>
/// <example>
/// [HasPermission(Permission.PatientsView)]
/// public async Task<IActionResult> GetPatients() { ... }
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Creates an authorization attribute that requires a specific permission
    /// </summary>
    /// <param name="permission">Required permission</param>
    public HasPermissionAttribute(Permission permission)
    {
        Policy = PermissionConstants.GetPolicyName(permission);
    }
}

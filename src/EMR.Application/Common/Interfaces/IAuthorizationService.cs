using EMR.Domain.Enums;

namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Authorization service for checking user permissions and resource access
/// Implements both role-based (RBAC) and attribute-based (ABAC) access control
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if the current user has a specific permission
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    Task<bool> HasPermissionAsync(Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific user has a permission
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="permission">Permission to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    Task<bool> UserHasPermissionAsync(Guid userId, Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has access to a specific resource
    /// </summary>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="resourceId">Resource identifier</param>
    /// <param name="permission">Required permission on the resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has access to the resource, false otherwise</returns>
    Task<bool> HasResourceAccessAsync(
        ResourceType resourceType,
        Guid resourceId,
        Permission permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific user has access to a resource
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="resourceId">Resource identifier</param>
    /// <param name="permission">Required permission on the resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has access to the resource, false otherwise</returns>
    Task<bool> UserHasResourceAccessAsync(
        Guid userId,
        ResourceType resourceType,
        Guid resourceId,
        Permission permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for the current user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of permissions</returns>
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a specific user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of permissions</returns>
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all resource IDs of a specific type that the current user has access to
    /// </summary>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="permission">Required permission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of authorized resource IDs</returns>
    Task<IEnumerable<Guid>> GetAuthorizedResourceIdsAsync(
        ResourceType resourceType,
        Permission permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and throws an exception if the current user doesn't have the required permission
    /// </summary>
    /// <param name="permission">Required permission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when user lacks permission</exception>
    Task RequirePermissionAsync(Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and throws an exception if the current user doesn't have access to the resource
    /// </summary>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="resourceId">Resource identifier</param>
    /// <param name="permission">Required permission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when user lacks access</exception>
    Task RequireResourceAccessAsync(
        ResourceType resourceType,
        Guid resourceId,
        Permission permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has any of the specified roles (async version - preferred)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="roles">Roles to check</param>
    /// <returns>True if user has any of the roles, false otherwise</returns>
    Task<bool> HasAnyRoleAsync(CancellationToken cancellationToken, params UserRole[] roles);

    /// <summary>
    /// Checks if the current user has any of the specified roles
    /// NOTE: Prefer HasAnyRoleAsync to avoid potential deadlocks
    /// </summary>
    /// <param name="roles">Roles to check</param>
    /// <returns>True if user has any of the roles, false otherwise</returns>
    bool HasAnyRole(params UserRole[] roles);

    /// <summary>
    /// Checks if the current user is an administrator (async version - preferred)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user is an admin, false otherwise</returns>
    Task<bool> IsAdminAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user is an administrator
    /// NOTE: Prefer IsAdminAsync to avoid potential deadlocks
    /// </summary>
    /// <returns>True if user is an admin, false otherwise</returns>
    bool IsAdmin();

    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    /// <returns>Current user ID or null if not authenticated</returns>
    Guid? GetCurrentUserId();

    /// <summary>
    /// Gets the current user's roles (async version - preferred)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of user roles</returns>
    Task<IEnumerable<UserRole>> GetCurrentUserRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user's roles
    /// NOTE: Prefer GetCurrentUserRolesAsync to avoid potential deadlocks
    /// </summary>
    /// <returns>Collection of user roles</returns>
    IEnumerable<UserRole> GetCurrentUserRoles();

    /// <summary>
    /// Invalidates the cached authorization data for a specific user
    /// Call this when user roles or permissions change
    /// </summary>
    /// <param name="userId">User identifier to invalidate cache for</param>
    void InvalidateUserCache(Guid userId);
}

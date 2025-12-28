using EMR.Domain.Entities;
using EMR.Domain.Enums;

namespace EMR.Domain.Interfaces;

/// <summary>
/// Repository interface for ResourceAuthorization entity operations
/// </summary>
public interface IResourceAuthorizationRepository : IRepository<ResourceAuthorization>
{
    /// <summary>
    /// Gets all active resource authorizations for a user
    /// </summary>
    Task<IEnumerable<ResourceAuthorization>> GetActiveAuthorizationsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active authorizations for a specific resource
    /// </summary>
    Task<IEnumerable<ResourceAuthorization>> GetActiveAuthorizationsForResourceAsync(
        ResourceType resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has permission on a specific resource
    /// </summary>
    Task<bool> HasResourcePermissionAsync(
        Guid userId,
        ResourceType resourceType,
        Guid resourceId,
        Permission permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all resources of a type that a user has access to with a specific permission
    /// </summary>
    Task<IEnumerable<Guid>> GetAuthorizedResourceIdsAsync(
        Guid userId,
        ResourceType resourceType,
        Permission permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all resource authorizations for a user on a specific resource
    /// </summary>
    Task RevokeResourceAccessAsync(
        Guid userId,
        ResourceType resourceType,
        Guid resourceId,
        string revokedBy,
        CancellationToken cancellationToken = default);
}

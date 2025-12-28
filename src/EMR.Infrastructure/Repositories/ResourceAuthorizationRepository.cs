using EMR.Application.Common.Interfaces;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Domain.Interfaces;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMR.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ResourceAuthorization entity
/// </summary>
public class ResourceAuthorizationRepository : Repository<ResourceAuthorization>, IResourceAuthorizationRepository
{
    public ResourceAuthorizationRepository(ApplicationDbContext context, ICurrentUserService currentUserService)
        : base(context, currentUserService)
    {
    }

    public async Task<IEnumerable<ResourceAuthorization>> GetActiveAuthorizationsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(ra =>
                ra.UserId == userId &&
                !ra.IsDeleted &&
                ra.EffectiveFrom <= now &&
                (!ra.EffectiveTo.HasValue || ra.EffectiveTo.Value >= now))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ResourceAuthorization>> GetActiveAuthorizationsForResourceAsync(
        ResourceType resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(ra =>
                ra.ResourceType == resourceType &&
                ra.ResourceId == resourceId &&
                !ra.IsDeleted &&
                ra.EffectiveFrom <= now &&
                (!ra.EffectiveTo.HasValue || ra.EffectiveTo.Value >= now))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasResourcePermissionAsync(
        Guid userId,
        ResourceType resourceType,
        Guid resourceId,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .AnyAsync(ra =>
                ra.UserId == userId &&
                ra.ResourceType == resourceType &&
                ra.ResourceId == resourceId &&
                ra.Permission == permission &&
                !ra.IsDeleted &&
                ra.EffectiveFrom <= now &&
                (!ra.EffectiveTo.HasValue || ra.EffectiveTo.Value >= now),
                cancellationToken);
    }

    public async Task<IEnumerable<Guid>> GetAuthorizedResourceIdsAsync(
        Guid userId,
        ResourceType resourceType,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(ra =>
                ra.UserId == userId &&
                ra.ResourceType == resourceType &&
                ra.Permission == permission &&
                !ra.IsDeleted &&
                ra.EffectiveFrom <= now &&
                (!ra.EffectiveTo.HasValue || ra.EffectiveTo.Value >= now))
            .Select(ra => ra.ResourceId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeResourceAccessAsync(
        Guid userId,
        ResourceType resourceType,
        Guid resourceId,
        string revokedBy,
        CancellationToken cancellationToken = default)
    {
        var authorizations = await _dbSet
            .Where(ra =>
                ra.UserId == userId &&
                ra.ResourceType == resourceType &&
                ra.ResourceId == resourceId &&
                !ra.IsDeleted &&
                ra.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var authorization in authorizations)
        {
            authorization.Revoke(revokedBy);
        }
    }
}

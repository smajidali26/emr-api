using EMR.Domain.Common;
using EMR.Domain.Enums;

namespace EMR.Domain.Entities;

/// <summary>
/// Represents resource-level authorization for attribute-based access control (ABAC)
/// Allows fine-grained access control where users can only access specific resource instances
/// Example: A doctor can only access patients assigned to them
/// </summary>
public class ResourceAuthorization : BaseEntity
{
    /// <summary>
    /// User identifier who is granted access
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Type of resource being protected
    /// </summary>
    public ResourceType ResourceType { get; private set; }

    /// <summary>
    /// Specific resource instance identifier
    /// Example: Patient ID, Encounter ID, etc.
    /// </summary>
    public Guid ResourceId { get; private set; }

    /// <summary>
    /// Permission granted on this specific resource
    /// </summary>
    public Permission Permission { get; private set; }

    /// <summary>
    /// Date and time when the authorization becomes effective (UTC)
    /// </summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>
    /// Date and time when the authorization expires (UTC)
    /// Null means no expiration
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    /// Reason for granting this specific resource access
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Checks if the resource authorization is currently active
    /// </summary>
    public bool IsActive
    {
        get
        {
            var now = DateTime.UtcNow;
            return !IsDeleted &&
                   now >= EffectiveFrom &&
                   (!EffectiveTo.HasValue || now <= EffectiveTo.Value);
        }
    }

    // Private constructor for EF Core
    private ResourceAuthorization() { }

    /// <summary>
    /// Creates a new resource authorization
    /// </summary>
    public ResourceAuthorization(
        Guid userId,
        ResourceType resourceType,
        Guid resourceId,
        Permission permission,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        string? reason,
        string grantedBy)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (resourceId == Guid.Empty)
            throw new ArgumentException("Resource ID cannot be empty", nameof(resourceId));

        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new ArgumentException("Effective-to date must be after effective-from date", nameof(effectiveTo));

        UserId = userId;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Permission = permission;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Reason = reason?.Trim();
        CreatedBy = grantedBy;
    }

    /// <summary>
    /// Updates the resource authorization expiration date
    /// </summary>
    public void UpdateExpiration(DateTime? effectiveTo, string updatedBy)
    {
        if (effectiveTo.HasValue && effectiveTo.Value <= EffectiveFrom)
            throw new ArgumentException("Effective-to date must be after effective-from date", nameof(effectiveTo));

        EffectiveTo = effectiveTo;
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Revokes the resource authorization immediately
    /// </summary>
    public void Revoke(string revokedBy)
    {
        EffectiveTo = DateTime.UtcNow;
        MarkAsUpdated(revokedBy);
    }
}

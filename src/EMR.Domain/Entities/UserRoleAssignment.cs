using EMR.Domain.Common;
using EMR.Domain.Enums;

namespace EMR.Domain.Entities;

/// <summary>
/// Maps users to roles with additional metadata
/// Provides audit trail for role assignments
/// </summary>
public class UserRoleAssignment : BaseEntity
{
    /// <summary>
    /// User identifier
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Role being assigned to the user
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Date and time when the role assignment becomes effective (UTC)
    /// </summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>
    /// Date and time when the role assignment expires (UTC)
    /// Null means no expiration
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    /// Reason for the role assignment
    /// </summary>
    public string? AssignmentReason { get; private set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Checks if the role assignment is currently active
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
    private UserRoleAssignment() { }

    /// <summary>
    /// Creates a new user-role assignment
    /// </summary>
    public UserRoleAssignment(
        Guid userId,
        UserRole role,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        string? assignmentReason,
        string assignedBy)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new ArgumentException("Effective-to date must be after effective-from date", nameof(effectiveTo));

        UserId = userId;
        Role = role;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        AssignmentReason = assignmentReason?.Trim();
        CreatedBy = assignedBy;
    }

    /// <summary>
    /// Updates the role assignment expiration date
    /// </summary>
    public void UpdateExpiration(DateTime? effectiveTo, string updatedBy)
    {
        if (effectiveTo.HasValue && effectiveTo.Value <= EffectiveFrom)
            throw new ArgumentException("Effective-to date must be after effective-from date", nameof(effectiveTo));

        EffectiveTo = effectiveTo;
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Revokes the role assignment immediately
    /// </summary>
    public void Revoke(string revokedBy)
    {
        EffectiveTo = DateTime.UtcNow;
        MarkAsUpdated(revokedBy);
    }
}

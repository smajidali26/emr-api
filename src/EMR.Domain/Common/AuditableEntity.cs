namespace EMR.Domain.Common;

/// <summary>
/// Base class for entities that require automatic audit trail generation
/// Extends BaseEntity with audit-specific tracking capabilities
/// Used for PHI-containing entities (Patient, Encounter, MedicalNote, etc.)
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>
    /// Indicates whether this entity contains Protected Health Information (PHI)
    /// When true, all access to this entity will be logged for HIPAA compliance
    /// </summary>
    public virtual bool ContainsPhi => true;

    /// <summary>
    /// Resource type name for audit logging (e.g., "Patient", "Encounter")
    /// Override in derived classes to provide specific resource type
    /// </summary>
    public virtual string AuditResourceType => GetType().Name;

    /// <summary>
    /// Resource identifier for audit logging (typically the entity's Id)
    /// </summary>
    public virtual string AuditResourceId => Id.ToString();

    /// <summary>
    /// Get a sanitized description of the entity for audit purposes
    /// Override in derived classes to provide meaningful audit trail
    /// IMPORTANT: Must not include PHI (no patient names, SSN, etc.)
    /// </summary>
    public virtual string GetAuditDescription()
    {
        return $"{AuditResourceType} (ID: {Id})";
    }

    /// <summary>
    /// Get fields that should be excluded from audit change tracking
    /// Override to specify fields that should not be tracked
    /// </summary>
    public virtual IEnumerable<string> GetAuditExcludedProperties()
    {
        return new[]
        {
            nameof(RowVersion),
            nameof(UpdatedAt),
            nameof(UpdatedBy)
        };
    }

    /// <summary>
    /// Determines if changes to this entity should trigger audit logging
    /// Override to implement custom audit rules
    /// </summary>
    public virtual bool ShouldAuditChanges()
    {
        return !IsDeleted; // Don't audit changes to deleted entities
    }
}

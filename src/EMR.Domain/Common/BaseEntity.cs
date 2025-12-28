namespace EMR.Domain.Common;

/// <summary>
/// Base class for all domain entities with audit fields
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Date and time when the entity was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// User identifier who created the entity
    /// </summary>
    public string CreatedBy { get; protected set; } = string.Empty;

    /// <summary>
    /// Date and time when the entity was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// User identifier who last updated the entity
    /// </summary>
    public string? UpdatedBy { get; protected set; }

    /// <summary>
    /// Indicates if the entity has been soft deleted
    /// </summary>
    public bool IsDeleted { get; protected set; }

    /// <summary>
    /// Date and time when the entity was deleted (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; protected set; }

    /// <summary>
    /// User identifier who deleted the entity
    /// </summary>
    public string? DeletedBy { get; protected set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control
    /// </summary>
    public byte[] RowVersion { get; protected set; } = Array.Empty<byte>();

    /// <summary>
    /// Mark entity as updated by a specific user
    /// </summary>
    public void MarkAsUpdated(string userId)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = userId;
    }

    /// <summary>
    /// Soft delete the entity
    /// </summary>
    public void MarkAsDeleted(string userId)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = userId;
    }

    /// <summary>
    /// Restore a soft-deleted entity
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}

namespace EMR.Domain.ReadModels;

/// <summary>
/// Base class for all read models (denormalized for queries)
/// </summary>
public abstract class BaseReadModel
{
    /// <summary>
    /// Unique identifier for the read model
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp when this read model was last updated
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version of the aggregate this read model represents
    /// Used for eventual consistency and optimistic concurrency
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Indicates if this read model is currently being rebuilt
    /// </summary>
    public bool IsRebuilding { get; set; }
}

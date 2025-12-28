namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// Entity representing a snapshot of an aggregate's state at a specific version.
/// Snapshots improve performance by avoiding replay of all events.
/// </summary>
public class SnapshotEntry
{
    /// <summary>
    /// Unique identifier for this snapshot
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The aggregate ID this snapshot belongs to
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// The type of aggregate
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Version of the aggregate when the snapshot was taken
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Serialized snapshot data (JSON)
    /// </summary>
    public string SnapshotData { get; set; } = string.Empty;

    /// <summary>
    /// When the snapshot was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Type of the snapshot class
    /// </summary>
    public string SnapshotType { get; set; } = string.Empty;
}

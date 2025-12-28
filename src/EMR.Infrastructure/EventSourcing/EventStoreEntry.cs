namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// Entity representing a stored event in the event store.
/// This is an immutable record of a domain event.
/// </summary>
public class EventStoreEntry
{
    /// <summary>
    /// Unique identifier for this event store entry
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Unique identifier of the event itself
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// The aggregate ID this event belongs to
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// The type of aggregate (e.g., "Patient", "Encounter")
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// The fully qualified type name of the event
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Version of the aggregate after this event was applied
    /// Used for optimistic concurrency control
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Version of the event schema
    /// </summary>
    public int EventVersion { get; set; }

    /// <summary>
    /// Serialized event data (JSON)
    /// </summary>
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the event occurred in the domain
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// When the event was persisted to the store
    /// </summary>
    public DateTime PersistedAt { get; set; }

    /// <summary>
    /// User who triggered the event
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Correlation ID for tracing related events
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Causation ID - the event/command that caused this event
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Sequence number for global ordering
    /// </summary>
    public long SequenceNumber { get; set; }
}

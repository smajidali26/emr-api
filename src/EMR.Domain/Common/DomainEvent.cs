namespace EMR.Domain.Common;

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// UTC timestamp when the event occurred
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Version of the event schema for handling event evolution
    /// </summary>
    public int EventVersion { get; protected set; } = 1;

    /// <summary>
    /// User who triggered the event (null for system events)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Correlation ID for tracing related events across aggregates
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Causation ID - the ID of the event/command that caused this event
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Additional metadata for the event
    /// </summary>
    public IDictionary<string, object>? Metadata { get; set; }
}

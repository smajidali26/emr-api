namespace EMR.Domain.Common;

/// <summary>
/// Base class for all domain events providing common event metadata.
/// Implements immutable event pattern - all properties are init-only.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// UTC timestamp when the event occurred
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Version of the event schema for handling event evolution
    /// Derived classes should override this when schema changes occur
    /// </summary>
    public virtual int EventVersion { get; init; } = 1;

    /// <summary>
    /// User who triggered the event (null for system events)
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Correlation ID for tracing related events across aggregates
    /// Use the same correlation ID for all events in a business transaction
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Causation ID - the ID of the event/command that caused this event
    /// Forms a causal chain: Command -> Event1 -> Event2 -> Event3
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Additional metadata for the event
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// The fully qualified name of the event type for serialization
    /// </summary>
    public string EventType => GetType().FullName ?? GetType().Name;

    protected DomainEventBase()
    {
    }

    /// <summary>
    /// Constructor with metadata initialization
    /// </summary>
    protected DomainEventBase(string? userId, string? correlationId = null, string? causationId = null)
    {
        UserId = userId;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        CausationId = causationId;
    }
}

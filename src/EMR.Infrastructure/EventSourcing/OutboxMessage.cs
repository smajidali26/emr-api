namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// Entity representing an outbox message for the transactional outbox pattern.
/// Ensures reliable event publishing by storing events in the same transaction as the aggregate.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier for this outbox message
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The event ID being published
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// The type of event
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized event data (JSON)
    /// </summary>
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// When the message was created in the outbox
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the message was processed (null if not yet processed)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Whether the message has been successfully published
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// Number of processing attempts
    /// </summary>
    public int ProcessingAttempts { get; set; }

    /// <summary>
    /// Last error message if processing failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Next scheduled retry time for failed messages
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }
}

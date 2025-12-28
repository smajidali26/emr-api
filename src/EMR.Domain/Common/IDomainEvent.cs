using MediatR;

namespace EMR.Domain.Common;

/// <summary>
/// Marker interface for all domain events in the system.
/// Domain events represent something that happened in the domain that domain experts care about.
/// Implements INotification to support MediatR notification handling.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// UTC timestamp when the event occurred
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Version of the event schema for handling event evolution
    /// </summary>
    int EventVersion { get; }

    /// <summary>
    /// User who triggered the event (null for system events)
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Correlation ID for tracing related events across aggregates
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Causation ID - the ID of the event/command that caused this event
    /// </summary>
    string? CausationId { get; }

    /// <summary>
    /// Additional metadata for the event
    /// </summary>
    IDictionary<string, object>? Metadata { get; }
}

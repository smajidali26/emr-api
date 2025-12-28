using EMR.Domain.Common;

namespace EMR.Application.Abstractions.EventSourcing;

/// <summary>
/// Interface for publishing domain events to external subscribers.
/// Decouples event persistence from event notification.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a single domain event to all registered handlers.
    /// </summary>
    /// <param name="domainEvent">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PublishAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events to all registered handlers.
    /// Events are published in the order they appear in the collection.
    /// </summary>
    /// <param name="domainEvents">Collection of events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PublishAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes events to external message bus for inter-service communication.
    /// Implements the Outbox pattern for reliable messaging.
    /// </summary>
    /// <param name="domainEvent">The event to publish externally</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PublishToMessageBusAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default);
}

using EMR.Domain.Common;

namespace EMR.Application.Abstractions.EventSourcing;

/// <summary>
/// Interface for the event store responsible for persisting and retrieving domain events.
/// Implements the Event Sourcing pattern for maintaining an immutable log of all domain events.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends events to the event stream for a specific aggregate.
    /// Implements optimistic concurrency control using expected version.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate</param>
    /// <param name="aggregateType">The type of the aggregate</param>
    /// <param name="events">Collection of events to append</param>
    /// <param name="expectedVersion">Expected version for optimistic concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    /// <exception cref="ConcurrencyException">Thrown when expected version doesn't match actual version</exception>
    Task AppendEventsAsync(
        Guid aggregateId,
        string aggregateType,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all events for a specific aggregate from the event stream.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate</param>
    /// <param name="fromVersion">Optional starting version (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of events for the aggregate</returns>
    Task<IEnumerable<IDomainEvent>> GetEventsAsync(
        Guid aggregateId,
        int? fromVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events for a specific aggregate up to a specific version.
    /// Useful for temporal queries and reconstructing historical state.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate</param>
    /// <param name="toVersion">Version to retrieve events up to (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of events up to the specified version</returns>
    Task<IEnumerable<IDomainEvent>> GetEventsUpToVersionAsync(
        Guid aggregateId,
        int toVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all events of a specific type across all aggregates.
    /// Useful for projections and read model building.
    /// </summary>
    /// <typeparam name="TEvent">Type of event to retrieve</typeparam>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of events of the specified type</returns>
    Task<IEnumerable<TEvent>> GetEventsByTypeAsync<TEvent>(
        CancellationToken cancellationToken = default) where TEvent : IDomainEvent;

    /// <summary>
    /// Retrieves events within a specific time range.
    /// Useful for audit and compliance reporting.
    /// </summary>
    /// <param name="startTime">Start of the time range (inclusive)</param>
    /// <param name="endTime">End of the time range (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of events within the time range</returns>
    Task<IEnumerable<IDomainEvent>> GetEventsByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events by correlation ID for tracing related events across aggregates.
    /// </summary>
    /// <param name="correlationId">The correlation ID to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of events with the specified correlation ID</returns>
    Task<IEnumerable<IDomainEvent>> GetEventsByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of an aggregate from the event store.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current version number, or 0 if aggregate doesn't exist</returns>
    Task<int> GetAggregateVersionAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an aggregate exists in the event store.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the aggregate exists, false otherwise</returns>
    Task<bool> AggregateExistsAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);
}

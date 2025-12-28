using EMR.Domain.Common;

namespace EMR.Application.Abstractions.EventSourcing;

/// <summary>
/// Interface for replaying events for debugging, projection rebuilding, and audit purposes.
/// </summary>
public interface IEventReplay
{
    /// <summary>
    /// Replays all events for a specific aggregate and reconstructs its state.
    /// </summary>
    /// <typeparam name="TAggregate">Type of the aggregate</typeparam>
    /// <param name="aggregateId">The unique identifier of the aggregate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reconstructed aggregate instance</returns>
    Task<TAggregate?> ReplayAggregateAsync<TAggregate>(
        Guid aggregateId,
        CancellationToken cancellationToken = default) where TAggregate : AggregateRoot, new();

    /// <summary>
    /// Replays events for an aggregate up to a specific point in time.
    /// Useful for temporal queries and "time travel" debugging.
    /// </summary>
    /// <typeparam name="TAggregate">Type of the aggregate</typeparam>
    /// <param name="aggregateId">The unique identifier of the aggregate</param>
    /// <param name="asOfDate">Point in time to replay to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reconstructed aggregate instance at the specified point in time</returns>
    Task<TAggregate?> ReplayAggregateAsOfAsync<TAggregate>(
        Guid aggregateId,
        DateTime asOfDate,
        CancellationToken cancellationToken = default) where TAggregate : AggregateRoot, new();

    /// <summary>
    /// Replays all events in the system to rebuild projections or read models.
    /// </summary>
    /// <param name="eventHandler">Handler to process each event</param>
    /// <param name="fromTimestamp">Optional starting timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task ReplayAllEventsAsync(
        Func<IDomainEvent, Task> eventHandler,
        DateTime? fromTimestamp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">Type of event to replay</typeparam>
    /// <param name="eventHandler">Handler to process each event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task ReplayEventsByTypeAsync<TEvent>(
        Func<TEvent, Task> eventHandler,
        CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
}

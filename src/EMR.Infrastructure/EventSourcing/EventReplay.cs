using EMR.Application.Abstractions.EventSourcing;
using EMR.Domain.Common;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// Implementation of event replay for aggregate reconstruction and projection rebuilding.
/// </summary>
public class EventReplay : IEventReplay
{
    private readonly EventStoreDbContext _context;
    private readonly IEventSerializer _serializer;
    private readonly ISnapshotStore _snapshotStore;
    private readonly ILogger<EventReplay> _logger;

    public EventReplay(
        EventStoreDbContext context,
        IEventSerializer serializer,
        ISnapshotStore snapshotStore,
        ILogger<EventReplay> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Replays all events for an aggregate to reconstruct its current state.
    /// Uses snapshots when available for performance optimization.
    /// </summary>
    public async Task<TAggregate?> ReplayAggregateAsync<TAggregate>(
        Guid aggregateId,
        CancellationToken cancellationToken = default) where TAggregate : AggregateRoot, new()
    {
        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));
        }

        try
        {
            var aggregate = new TAggregate();
            var fromVersion = 0;

            // Try to load from snapshot first
            var snapshotResult = await _snapshotStore.GetSnapshotAsync<TAggregate>(aggregateId, cancellationToken);
            if (snapshotResult.HasValue)
            {
                aggregate = snapshotResult.Value.Snapshot;
                fromVersion = snapshotResult.Value.Version;

                _logger.LogDebug(
                    "Loaded snapshot for aggregate {AggregateId} at version {Version}",
                    aggregateId,
                    fromVersion);
            }

            // Load events from snapshot version onwards
            var events = await _context.EventStore
                .Where(e => e.AggregateId == aggregateId && e.Version > fromVersion)
                .OrderBy(e => e.Version)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (!events.Any() && fromVersion == 0)
            {
                _logger.LogWarning(
                    "No events found for aggregate {AggregateId}",
                    aggregateId);
                return null;
            }

            // Replay events
            var domainEvents = events.Select(e => _serializer.Deserialize(e.EventData, e.EventType));
            if (aggregate != null)
            {
                aggregate.LoadFromHistory(domainEvents);
            }

            _logger.LogInformation(
                "Replayed {EventCount} events for aggregate {AggregateId} (current version: {Version})",
                events.Count,
                aggregateId,
                aggregate?.Version ?? 0);

            return aggregate;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error replaying aggregate {AggregateId}",
                aggregateId);
            throw;
        }
    }

    /// <summary>
    /// Replays events for an aggregate up to a specific point in time (temporal query).
    /// </summary>
    public async Task<TAggregate?> ReplayAggregateAsOfAsync<TAggregate>(
        Guid aggregateId,
        DateTime asOfDate,
        CancellationToken cancellationToken = default) where TAggregate : AggregateRoot, new()
    {
        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));
        }

        try
        {
            var aggregate = new TAggregate();

            // Load events up to the specified date
            var events = await _context.EventStore
                .Where(e => e.AggregateId == aggregateId && e.OccurredAt <= asOfDate)
                .OrderBy(e => e.Version)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (!events.Any())
            {
                _logger.LogWarning(
                    "No events found for aggregate {AggregateId} as of {AsOfDate}",
                    aggregateId,
                    asOfDate);
                return null;
            }

            // Replay events
            var domainEvents = events.Select(e => _serializer.Deserialize(e.EventData, e.EventType));
            aggregate.LoadFromHistory(domainEvents);

            _logger.LogInformation(
                "Replayed {EventCount} events for aggregate {AggregateId} as of {AsOfDate} (version: {Version})",
                events.Count,
                aggregateId,
                asOfDate,
                aggregate.Version);

            return aggregate;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error replaying aggregate {AggregateId} as of {AsOfDate}",
                aggregateId,
                asOfDate);
            throw;
        }
    }

    /// <summary>
    /// Replays all events in the system for projection rebuilding.
    /// </summary>
    public async Task ReplayAllEventsAsync(
        Func<IDomainEvent, Task> eventHandler,
        DateTime? fromTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        if (eventHandler == null)
        {
            throw new ArgumentNullException(nameof(eventHandler));
        }

        try
        {
            _logger.LogInformation(
                "Starting replay of all events{FromClause}",
                fromTimestamp.HasValue ? $" from {fromTimestamp.Value}" : string.Empty);

            var query = _context.EventStore.AsNoTracking().OrderBy(e => e.SequenceNumber);

            if (fromTimestamp.HasValue)
            {
                query = (IOrderedQueryable<EventStoreEntry>)query.Where(e => e.OccurredAt >= fromTimestamp.Value);
            }

            var batchSize = 1000;
            var processedCount = 0;
            var hasMore = true;
            long lastSequenceNumber = 0;

            while (hasMore && !cancellationToken.IsCancellationRequested)
            {
                var batch = await query
                    .Where(e => e.SequenceNumber > lastSequenceNumber)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!batch.Any())
                {
                    hasMore = false;
                    break;
                }

                foreach (var entry in batch)
                {
                    var domainEvent = _serializer.Deserialize(entry.EventData, entry.EventType);
                    await eventHandler(domainEvent);
                    lastSequenceNumber = entry.SequenceNumber;
                    processedCount++;
                }

                _logger.LogDebug(
                    "Processed {ProcessedCount} events in replay",
                    processedCount);
            }

            _logger.LogInformation(
                "Completed replay of {TotalCount} events",
                processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replaying all events");
            throw;
        }
    }

    /// <summary>
    /// Replays events of a specific type for targeted projection rebuilding.
    /// </summary>
    public async Task ReplayEventsByTypeAsync<TEvent>(
        Func<TEvent, Task> eventHandler,
        CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        if (eventHandler == null)
        {
            throw new ArgumentNullException(nameof(eventHandler));
        }

        var eventTypeName = typeof(TEvent).FullName ?? typeof(TEvent).Name;

        try
        {
            _logger.LogInformation(
                "Starting replay of events of type {EventType}",
                eventTypeName);

            var events = await _context.EventStore
                .Where(e => e.EventType == eventTypeName)
                .OrderBy(e => e.SequenceNumber)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var entry in events)
            {
                var domainEvent = _serializer.Deserialize<TEvent>(entry.EventData);
                await eventHandler(domainEvent);
            }

            _logger.LogInformation(
                "Completed replay of {EventCount} events of type {EventType}",
                events.Count,
                eventTypeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error replaying events of type {EventType}",
                eventTypeName);
            throw;
        }
    }
}

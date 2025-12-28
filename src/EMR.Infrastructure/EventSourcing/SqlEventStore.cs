using EMR.Application.Abstractions.EventSourcing;
using EMR.Domain.Common;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// SQL-based implementation of the event store.
/// Provides durable storage for domain events with optimistic concurrency control.
/// </summary>
public class SqlEventStore : IEventStore
{
    private readonly EventStoreDbContext _context;
    private readonly IEventSerializer _serializer;
    private readonly ILogger<SqlEventStore> _logger;

    public SqlEventStore(
        EventStoreDbContext context,
        IEventSerializer serializer,
        ILogger<SqlEventStore> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Appends events to the event stream with optimistic concurrency control.
    /// </summary>
    public async Task AppendEventsAsync(
        Guid aggregateId,
        string aggregateType,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));
        }

        if (string.IsNullOrWhiteSpace(aggregateType))
        {
            throw new ArgumentNullException(nameof(aggregateType));
        }

        var eventsList = events?.ToList() ?? throw new ArgumentNullException(nameof(events));
        if (!eventsList.Any())
        {
            return;
        }

        try
        {
            // Check current version for optimistic concurrency
            var currentVersion = await GetAggregateVersionAsync(aggregateId, cancellationToken);

            if (currentVersion != expectedVersion)
            {
                throw new ConcurrencyException(aggregateId, expectedVersion, currentVersion);
            }

            var version = expectedVersion;
            var now = DateTime.UtcNow;

            foreach (var domainEvent in eventsList)
            {
                version++;

                var eventEntry = new EventStoreEntry
                {
                    EventId = domainEvent.EventId,
                    AggregateId = aggregateId,
                    AggregateType = aggregateType,
                    EventType = domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
                    Version = version,
                    EventVersion = domainEvent.EventVersion,
                    EventData = _serializer.Serialize(domainEvent),
                    Metadata = SerializeMetadata(domainEvent.Metadata),
                    OccurredAt = domainEvent.OccurredAt,
                    PersistedAt = now,
                    UserId = domainEvent.UserId,
                    CorrelationId = domainEvent.CorrelationId,
                    CausationId = domainEvent.CausationId
                };

                await _context.EventStore.AddAsync(eventEntry, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Appended {EventCount} events to aggregate {AggregateId} (version {Version})",
                eventsList.Count,
                aggregateId,
                version);
        }
        catch (DbUpdateException ex) when (IsConcurrencyException(ex))
        {
            // Handle database-level concurrency conflicts
            var currentVersion = await GetAggregateVersionAsync(aggregateId, cancellationToken);
            throw new ConcurrencyException(aggregateId, expectedVersion, currentVersion, "Concurrency conflict detected.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error appending events to aggregate {AggregateId}",
                aggregateId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all events for an aggregate.
    /// </summary>
    public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(
        Guid aggregateId,
        int? fromVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));
        }

        var query = _context.EventStore
            .Where(e => e.AggregateId == aggregateId)
            .AsNoTracking();

        if (fromVersion.HasValue)
        {
            query = query.Where(e => e.Version >= fromVersion.Value);
        }

        var entries = await query
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        return entries.Select(e => _serializer.Deserialize(e.EventData, e.EventType));
    }

    /// <summary>
    /// Retrieves events up to a specific version for temporal queries.
    /// </summary>
    public async Task<IEnumerable<IDomainEvent>> GetEventsUpToVersionAsync(
        Guid aggregateId,
        int toVersion,
        CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));
        }

        var entries = await _context.EventStore
            .Where(e => e.AggregateId == aggregateId && e.Version <= toVersion)
            .OrderBy(e => e.Version)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entries.Select(e => _serializer.Deserialize(e.EventData, e.EventType));
    }

    /// <summary>
    /// Retrieves all events of a specific type for projections.
    /// </summary>
    public async Task<IEnumerable<TEvent>> GetEventsByTypeAsync<TEvent>(
        CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        var eventTypeName = typeof(TEvent).FullName ?? typeof(TEvent).Name;

        var entries = await _context.EventStore
            .Where(e => e.EventType == eventTypeName)
            .OrderBy(e => e.SequenceNumber)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entries.Select(e => _serializer.Deserialize<TEvent>(e.EventData));
    }

    /// <summary>
    /// Retrieves events within a time range for audit purposes.
    /// </summary>
    public async Task<IEnumerable<IDomainEvent>> GetEventsByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        var entries = await _context.EventStore
            .Where(e => e.OccurredAt >= startTime && e.OccurredAt <= endTime)
            .OrderBy(e => e.SequenceNumber)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entries.Select(e => _serializer.Deserialize(e.EventData, e.EventType));
    }

    /// <summary>
    /// Retrieves events by correlation ID for tracing.
    /// </summary>
    public async Task<IEnumerable<IDomainEvent>> GetEventsByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentNullException(nameof(correlationId));
        }

        var entries = await _context.EventStore
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.SequenceNumber)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entries.Select(e => _serializer.Deserialize(e.EventData, e.EventType));
    }

    /// <summary>
    /// Gets the current version of an aggregate.
    /// </summary>
    public async Task<int> GetAggregateVersionAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));
        }

        var maxVersion = await _context.EventStore
            .Where(e => e.AggregateId == aggregateId)
            .MaxAsync(e => (int?)e.Version, cancellationToken);

        return maxVersion ?? 0;
    }

    /// <summary>
    /// Checks if an aggregate exists in the event store.
    /// </summary>
    public async Task<bool> AggregateExistsAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
        {
            return false;
        }

        return await _context.EventStore
            .AnyAsync(e => e.AggregateId == aggregateId, cancellationToken);
    }

    private string? SerializeMetadata(IDictionary<string, object>? metadata)
    {
        if (metadata == null || !metadata.Any())
        {
            return null;
        }

        return System.Text.Json.JsonSerializer.Serialize(metadata);
    }

    private bool IsConcurrencyException(DbUpdateException ex)
    {
        // Check for PostgreSQL unique constraint violation (duplicate version)
        return ex.InnerException?.Message.Contains("duplicate key") == true ||
               ex.InnerException?.Message.Contains("IX_EventStore_AggregateId_Version") == true;
    }
}

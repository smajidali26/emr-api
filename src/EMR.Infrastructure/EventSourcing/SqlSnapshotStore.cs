using EMR.Application.Abstractions.EventSourcing;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// SQL-based implementation of the snapshot store.
/// Provides performance optimization for event replay by storing aggregate snapshots.
/// </summary>
public class SqlSnapshotStore : ISnapshotStore
{
    private readonly EventStoreDbContext _context;
    private readonly ILogger<SqlSnapshotStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private const int DefaultSnapshotInterval = 50; // Take snapshot every N events

    public SqlSnapshotStore(
        EventStoreDbContext context,
        ILogger<SqlSnapshotStore> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Saves a snapshot of an aggregate.
    /// </summary>
    public async Task SaveSnapshotAsync<TSnapshot>(
        Guid aggregateId,
        TSnapshot snapshot,
        int version,
        CancellationToken cancellationToken = default) where TSnapshot : class
    {
        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));
        }

        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        try
        {
            var snapshotData = JsonSerializer.Serialize(snapshot, _jsonOptions);
            var aggregateType = snapshot.GetType().Name.Replace("Snapshot", string.Empty);

            var snapshotEntry = new SnapshotEntry
            {
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                Version = version,
                SnapshotData = snapshotData,
                SnapshotType = typeof(TSnapshot).FullName ?? typeof(TSnapshot).Name,
                CreatedAt = DateTime.UtcNow
            };

            // Delete old snapshots for this aggregate to save space
            var oldSnapshots = await _context.Snapshots
                .Where(s => s.AggregateId == aggregateId)
                .ToListAsync(cancellationToken);

            if (oldSnapshots.Any())
            {
                _context.Snapshots.RemoveRange(oldSnapshots);
            }

            await _context.Snapshots.AddAsync(snapshotEntry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Saved snapshot for aggregate {AggregateId} at version {Version}",
                aggregateId,
                version);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error saving snapshot for aggregate {AggregateId} at version {Version}",
                aggregateId,
                version);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the most recent snapshot for an aggregate.
    /// </summary>
    public async Task<(TSnapshot? Snapshot, int Version)?> GetSnapshotAsync<TSnapshot>(
        Guid aggregateId,
        CancellationToken cancellationToken = default) where TSnapshot : class
    {
        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));
        }

        try
        {
            var snapshotEntry = await _context.Snapshots
                .Where(s => s.AggregateId == aggregateId)
                .OrderByDescending(s => s.Version)
                .FirstOrDefaultAsync(cancellationToken);

            if (snapshotEntry == null)
            {
                return null;
            }

            var snapshot = JsonSerializer.Deserialize<TSnapshot>(
                snapshotEntry.SnapshotData,
                _jsonOptions);

            if (snapshot == null)
            {
                _logger.LogWarning(
                    "Failed to deserialize snapshot for aggregate {AggregateId}",
                    aggregateId);
                return null;
            }

            _logger.LogDebug(
                "Retrieved snapshot for aggregate {AggregateId} at version {Version}",
                aggregateId,
                snapshotEntry.Version);

            return (snapshot, snapshotEntry.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving snapshot for aggregate {AggregateId}",
                aggregateId);
            throw;
        }
    }

    /// <summary>
    /// Deletes all snapshots for an aggregate.
    /// </summary>
    public async Task DeleteSnapshotsAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));
        }

        try
        {
            var snapshots = await _context.Snapshots
                .Where(s => s.AggregateId == aggregateId)
                .ToListAsync(cancellationToken);

            if (snapshots.Any())
            {
                _context.Snapshots.RemoveRange(snapshots);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Deleted {SnapshotCount} snapshots for aggregate {AggregateId}",
                    snapshots.Count,
                    aggregateId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting snapshots for aggregate {AggregateId}",
                aggregateId);
            throw;
        }
    }

    /// <summary>
    /// Determines if a snapshot should be taken based on version interval.
    /// </summary>
    public bool ShouldTakeSnapshot(int currentVersion, int lastSnapshotVersion)
    {
        // Take snapshot every N events
        return (currentVersion - lastSnapshotVersion) >= DefaultSnapshotInterval;
    }
}

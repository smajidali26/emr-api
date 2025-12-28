namespace EMR.Application.Abstractions.EventSourcing;

/// <summary>
/// Interface for storing and retrieving aggregate snapshots to optimize event replay performance.
/// Snapshots capture the state of an aggregate at a specific version.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Saves a snapshot of an aggregate's state at a specific version.
    /// </summary>
    /// <typeparam name="TSnapshot">Type of the snapshot</typeparam>
    /// <param name="aggregateId">The unique identifier of the aggregate</param>
    /// <param name="snapshot">The snapshot data</param>
    /// <param name="version">The version of the aggregate when the snapshot was taken</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task SaveSnapshotAsync<TSnapshot>(
        Guid aggregateId,
        TSnapshot snapshot,
        int version,
        CancellationToken cancellationToken = default) where TSnapshot : class;

    /// <summary>
    /// Retrieves the most recent snapshot for an aggregate.
    /// </summary>
    /// <typeparam name="TSnapshot">Type of the snapshot</typeparam>
    /// <param name="aggregateId">The unique identifier of the aggregate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Snapshot and its version, or null if no snapshot exists</returns>
    Task<(TSnapshot? Snapshot, int Version)?> GetSnapshotAsync<TSnapshot>(
        Guid aggregateId,
        CancellationToken cancellationToken = default) where TSnapshot : class;

    /// <summary>
    /// Deletes all snapshots for an aggregate (typically when aggregate is deleted).
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task DeleteSnapshotsAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a snapshot should be taken based on configured strategy.
    /// Common strategies: every N events, time-based, or size-based.
    /// </summary>
    /// <param name="currentVersion">Current version of the aggregate</param>
    /// <param name="lastSnapshotVersion">Version of the last snapshot (0 if none)</param>
    /// <returns>True if a snapshot should be taken</returns>
    bool ShouldTakeSnapshot(int currentVersion, int lastSnapshotVersion);
}

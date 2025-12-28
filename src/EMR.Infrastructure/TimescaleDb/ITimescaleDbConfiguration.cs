namespace EMR.Infrastructure.TimescaleDb;

/// <summary>
/// Interface for TimescaleDB configuration and management operations
/// Provides hypertable management, compression, and retention policy controls
/// </summary>
public interface ITimescaleDbConfiguration
{
    /// <summary>
    /// Initialize hypertable if not already configured
    /// Should be called during application startup
    /// </summary>
    Task InitializeHypertableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get information about the AuditLogs hypertable
    /// </summary>
    Task<HypertableInfo> GetHypertableInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get compression statistics for the AuditLogs hypertable
    /// </summary>
    Task<CompressionStats> GetCompressionStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get storage statistics including chunk information
    /// </summary>
    Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check HIPAA retention compliance status
    /// </summary>
    Task<RetentionComplianceStatus> CheckRetentionComplianceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually trigger compression for chunks older than specified age
    /// </summary>
    Task<int> CompressOldChunksAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh continuous aggregates
    /// </summary>
    Task RefreshContinuousAggregatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chunk information for monitoring
    /// </summary>
    Task<IReadOnlyList<ChunkInfo>> GetChunkInfoAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about the hypertable configuration
/// </summary>
public record HypertableInfo
{
    public required string HypertableName { get; init; }
    public required string SchemaName { get; init; }
    public required bool IsHypertable { get; init; }
    public required TimeSpan ChunkTimeInterval { get; init; }
    public required int NumChunks { get; init; }
    public required long TotalRows { get; init; }
    public required bool CompressionEnabled { get; init; }
    public required bool RetentionPolicyEnabled { get; init; }
    public required int RetentionDays { get; init; }
}

/// <summary>
/// Compression statistics for the hypertable
/// </summary>
public record CompressionStats
{
    public required long UncompressedBytes { get; init; }
    public required long CompressedBytes { get; init; }
    public required double CompressionRatio { get; init; }
    public required int CompressedChunks { get; init; }
    public required int UncompressedChunks { get; init; }
    public required long RowsCompressed { get; init; }
    public required long RowsUncompressed { get; init; }
    public required DateTime? LastCompressionRun { get; init; }
}

/// <summary>
/// Storage statistics for the hypertable
/// </summary>
public record StorageStats
{
    public required long TotalBytes { get; init; }
    public required long TableBytes { get; init; }
    public required long IndexBytes { get; init; }
    public required long ToastBytes { get; init; }
    public required string TotalSizeFormatted { get; init; }
    public required int ActiveChunks { get; init; }
    public required DateTime? OldestRecord { get; init; }
    public required DateTime? NewestRecord { get; init; }
    public required long TotalRecords { get; init; }
}

/// <summary>
/// HIPAA retention compliance status
/// </summary>
public record RetentionComplianceStatus
{
    public required bool IsCompliant { get; init; }
    public required string ComplianceMessage { get; init; }
    public required DateTime? EarliestRecord { get; init; }
    public required DateTime? LatestRecord { get; init; }
    public required long TotalRecords { get; init; }
    public required int RetentionDays { get; init; }
    public required int ActualRetentionDays { get; init; }
    public required DateTime? NextRetentionPolicyRun { get; init; }
}

/// <summary>
/// Information about a single chunk
/// </summary>
public record ChunkInfo
{
    public required string ChunkName { get; init; }
    public required string SchemaName { get; init; }
    public required DateTime RangeStart { get; init; }
    public required DateTime RangeEnd { get; init; }
    public required bool IsCompressed { get; init; }
    public required long RowCount { get; init; }
    public required long SizeBytes { get; init; }
    public required string SizeFormatted { get; init; }
}

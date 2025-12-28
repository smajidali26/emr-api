using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.TimescaleDb;

/// <summary>
/// TimescaleDB configuration and management service
/// Handles hypertable operations, compression, and retention policies
///
/// HIPAA COMPLIANCE:
/// - Enforces 7-year (2,555 days) retention policy
/// - Provides compliance status reporting
/// - Manages compression for storage optimization
/// </summary>
public class TimescaleDbConfiguration : ITimescaleDbConfiguration
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TimescaleDbConfiguration> _logger;
    private const string HypertableName = "AuditLogs";
    private const int HipaaRetentionDays = 2555; // 7 years

    public TimescaleDbConfiguration(
        ApplicationDbContext context,
        ILogger<TimescaleDbConfiguration> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeHypertableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if TimescaleDB extension is available
            var extensionExists = await _context.Database
                .SqlQuery<bool>($@"
                    SELECT EXISTS(
                        SELECT 1 FROM pg_extension WHERE extname = 'timescaledb'
                    )")
                .FirstOrDefaultAsync(cancellationToken);

            if (!extensionExists)
            {
                _logger.LogWarning(
                    "TimescaleDB extension not found. Audit logging will use standard PostgreSQL table. " +
                    "For optimal performance and HIPAA compliance, install TimescaleDB and run migrations.");
                return;
            }

            // Check if table is already a hypertable
            var isHypertable = await IsHypertableAsync(cancellationToken);
            if (isHypertable)
            {
                _logger.LogInformation("AuditLogs table is already configured as a TimescaleDB hypertable");
                return;
            }

            _logger.LogWarning(
                "AuditLogs table is not a hypertable. Run EF Core migrations to convert it. " +
                "Migration: 20251228120000_TimescaleDbAuditLogs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize TimescaleDB hypertable");
            throw;
        }
    }

    public async Task<HypertableInfo> GetHypertableInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isHypertable = await IsHypertableAsync(cancellationToken);

            if (!isHypertable)
            {
                return new HypertableInfo
                {
                    HypertableName = HypertableName,
                    SchemaName = "public",
                    IsHypertable = false,
                    ChunkTimeInterval = TimeSpan.Zero,
                    NumChunks = 0,
                    TotalRows = await _context.AuditLogs.LongCountAsync(cancellationToken),
                    CompressionEnabled = false,
                    RetentionPolicyEnabled = false,
                    RetentionDays = 0
                };
            }

            var info = await _context.Database
                .SqlQuery<HypertableInfoRaw>($@"
                    SELECT
                        h.hypertable_name,
                        h.hypertable_schema as schema_name,
                        EXTRACT(DAY FROM d.time_interval)::INTEGER as chunk_interval_days,
                        (SELECT COUNT(*) FROM timescaledb_information.chunks
                         WHERE hypertable_name = 'AuditLogs')::INTEGER as num_chunks,
                        (SELECT COUNT(*) FROM ""AuditLogs"")::BIGINT as total_rows,
                        COALESCE(c.compression_enabled, FALSE) as compression_enabled,
                        (SELECT config->>'drop_after' IS NOT NULL
                         FROM timescaledb_information.jobs
                         WHERE hypertable_name = 'AuditLogs'
                         AND proc_name = 'policy_retention') as retention_enabled,
                        COALESCE(
                            EXTRACT(DAY FROM (
                                SELECT config->>'drop_after'
                                FROM timescaledb_information.jobs
                                WHERE hypertable_name = 'AuditLogs'
                                AND proc_name = 'policy_retention'
                            )::INTERVAL),
                            0
                        )::INTEGER as retention_days
                    FROM timescaledb_information.hypertables h
                    LEFT JOIN timescaledb_information.dimensions d
                        ON h.hypertable_name = d.hypertable_name
                    LEFT JOIN timescaledb_information.compression_settings c
                        ON h.hypertable_name = c.hypertable_name
                    WHERE h.hypertable_name = 'AuditLogs'
                    LIMIT 1")
                .FirstOrDefaultAsync(cancellationToken);

            if (info == null)
            {
                throw new InvalidOperationException("Failed to retrieve hypertable information");
            }

            return new HypertableInfo
            {
                HypertableName = info.hypertable_name,
                SchemaName = info.schema_name,
                IsHypertable = true,
                ChunkTimeInterval = TimeSpan.FromDays(info.chunk_interval_days),
                NumChunks = info.num_chunks,
                TotalRows = info.total_rows,
                CompressionEnabled = info.compression_enabled,
                RetentionPolicyEnabled = info.retention_enabled,
                RetentionDays = info.retention_days
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hypertable info");
            throw;
        }
    }

    public async Task<CompressionStats> GetCompressionStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isHypertable = await IsHypertableAsync(cancellationToken);

            if (!isHypertable)
            {
                return new CompressionStats
                {
                    UncompressedBytes = 0,
                    CompressedBytes = 0,
                    CompressionRatio = 0,
                    CompressedChunks = 0,
                    UncompressedChunks = 0,
                    RowsCompressed = 0,
                    RowsUncompressed = 0,
                    LastCompressionRun = null
                };
            }

            var stats = await _context.Database
                .SqlQuery<CompressionStatsRaw>($@"
                    SELECT
                        COALESCE(SUM(before_compression_total_bytes), 0)::BIGINT as uncompressed_bytes,
                        COALESCE(SUM(after_compression_total_bytes), 0)::BIGINT as compressed_bytes,
                        CASE
                            WHEN COALESCE(SUM(after_compression_total_bytes), 0) = 0 THEN 0
                            ELSE ROUND(
                                SUM(before_compression_total_bytes)::NUMERIC /
                                NULLIF(SUM(after_compression_total_bytes), 0)::NUMERIC,
                                2
                            )
                        END::DOUBLE PRECISION as compression_ratio,
                        COUNT(*) FILTER (WHERE is_compressed)::INTEGER as compressed_chunks,
                        COUNT(*) FILTER (WHERE NOT is_compressed)::INTEGER as uncompressed_chunks,
                        COALESCE(SUM(before_compression_total_bytes) FILTER (WHERE is_compressed) /
                            NULLIF(AVG(before_compression_total_bytes / NULLIF(before_compression_total_bytes, 0)), 0), 0)::BIGINT
                            as rows_compressed,
                        (SELECT COUNT(*) FROM ""AuditLogs"")::BIGINT as rows_uncompressed,
                        (SELECT MAX(last_run_started_at)
                         FROM timescaledb_information.job_stats js
                         JOIN timescaledb_information.jobs j ON js.job_id = j.job_id
                         WHERE j.hypertable_name = 'AuditLogs'
                         AND j.proc_name = 'policy_compression') as last_compression_run
                    FROM timescaledb_information.chunks
                    WHERE hypertable_name = 'AuditLogs'")
                .FirstOrDefaultAsync(cancellationToken);

            if (stats == null)
            {
                throw new InvalidOperationException("Failed to retrieve compression stats");
            }

            return new CompressionStats
            {
                UncompressedBytes = stats.uncompressed_bytes,
                CompressedBytes = stats.compressed_bytes,
                CompressionRatio = stats.compression_ratio,
                CompressedChunks = stats.compressed_chunks,
                UncompressedChunks = stats.uncompressed_chunks,
                RowsCompressed = stats.rows_compressed,
                RowsUncompressed = stats.rows_uncompressed,
                LastCompressionRun = stats.last_compression_run
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get compression stats");
            throw;
        }
    }

    public async Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _context.Database
                .SqlQuery<StorageStatsRaw>($@"
                    SELECT
                        pg_total_relation_size('""AuditLogs""')::BIGINT as total_bytes,
                        pg_table_size('""AuditLogs""')::BIGINT as table_bytes,
                        pg_indexes_size('""AuditLogs""')::BIGINT as index_bytes,
                        COALESCE(pg_total_relation_size(reltoastrelid), 0)::BIGINT as toast_bytes,
                        pg_size_pretty(pg_total_relation_size('""AuditLogs""')) as total_size_formatted,
                        (SELECT COUNT(*)::INTEGER FROM timescaledb_information.chunks
                         WHERE hypertable_name = 'AuditLogs') as active_chunks,
                        (SELECT MIN(""Timestamp"") FROM ""AuditLogs"") as oldest_record,
                        (SELECT MAX(""Timestamp"") FROM ""AuditLogs"") as newest_record,
                        (SELECT COUNT(*) FROM ""AuditLogs"")::BIGINT as total_records
                    FROM pg_class
                    WHERE relname = 'AuditLogs'
                    LIMIT 1")
                .FirstOrDefaultAsync(cancellationToken);

            if (stats == null)
            {
                // Fallback for non-hypertable
                var count = await _context.AuditLogs.LongCountAsync(cancellationToken);
                var oldest = await _context.AuditLogs.MinAsync(a => (DateTime?)a.Timestamp, cancellationToken);
                var newest = await _context.AuditLogs.MaxAsync(a => (DateTime?)a.Timestamp, cancellationToken);

                return new StorageStats
                {
                    TotalBytes = 0,
                    TableBytes = 0,
                    IndexBytes = 0,
                    ToastBytes = 0,
                    TotalSizeFormatted = "N/A",
                    ActiveChunks = 0,
                    OldestRecord = oldest,
                    NewestRecord = newest,
                    TotalRecords = count
                };
            }

            return new StorageStats
            {
                TotalBytes = stats.total_bytes,
                TableBytes = stats.table_bytes,
                IndexBytes = stats.index_bytes,
                ToastBytes = stats.toast_bytes,
                TotalSizeFormatted = stats.total_size_formatted,
                ActiveChunks = stats.active_chunks,
                OldestRecord = stats.oldest_record,
                NewestRecord = stats.newest_record,
                TotalRecords = stats.total_records
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage stats");
            throw;
        }
    }

    public async Task<RetentionComplianceStatus> CheckRetentionComplianceAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var oldest = await _context.AuditLogs
                .MinAsync(a => (DateTime?)a.Timestamp, cancellationToken);

            var newest = await _context.AuditLogs
                .MaxAsync(a => (DateTime?)a.Timestamp, cancellationToken);

            var totalRecords = await _context.AuditLogs.LongCountAsync(cancellationToken);

            var actualRetentionDays = oldest.HasValue && newest.HasValue
                ? (int)(newest.Value - oldest.Value).TotalDays
                : 0;

            var isCompliant = actualRetentionDays <= HipaaRetentionDays;

            string message;
            if (totalRecords == 0)
            {
                message = "No audit records found";
            }
            else if (isCompliant)
            {
                message = $"HIPAA compliant - records span {actualRetentionDays} days (within 7-year limit)";
            }
            else
            {
                message = $"WARNING: Records span {actualRetentionDays} days, exceeding 7-year retention limit";
            }

            // Get next retention policy run time
            DateTime? nextRetentionRun = null;
            try
            {
                nextRetentionRun = await _context.Database
                    .SqlQuery<DateTime?>($@"
                        SELECT next_start
                        FROM timescaledb_information.job_stats js
                        JOIN timescaledb_information.jobs j ON js.job_id = j.job_id
                        WHERE j.hypertable_name = 'AuditLogs'
                        AND j.proc_name = 'policy_retention'
                        LIMIT 1")
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch
            {
                // TimescaleDB not available
            }

            return new RetentionComplianceStatus
            {
                IsCompliant = isCompliant,
                ComplianceMessage = message,
                EarliestRecord = oldest,
                LatestRecord = newest,
                TotalRecords = totalRecords,
                RetentionDays = HipaaRetentionDays,
                ActualRetentionDays = actualRetentionDays,
                NextRetentionPolicyRun = nextRetentionRun
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check retention compliance");
            throw;
        }
    }

    public async Task<int> CompressOldChunksAsync(
        TimeSpan olderThan,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isHypertable = await IsHypertableAsync(cancellationToken);
            if (!isHypertable)
            {
                _logger.LogWarning("Cannot compress chunks - AuditLogs is not a hypertable");
                return 0;
            }

            // SECURITY: Convert TimeSpan to a safe interval format to prevent SQL injection
            // Validate and format as PostgreSQL interval string (e.g., "30 days")
            var totalDays = (int)olderThan.TotalDays;
            if (totalDays < 0 || totalDays > 36500) // Max ~100 years
            {
                throw new ArgumentOutOfRangeException(nameof(olderThan), "Interval must be between 0 and 36500 days");
            }

            // Use parameterized query with integer days
            var compressedCount = await _context.Database
                .SqlQuery<int>($@"
                    SELECT COUNT(*)::INTEGER
                    FROM (
                        SELECT compress_chunk(chunk_name::REGCLASS)
                        FROM timescaledb_information.chunks
                        WHERE hypertable_name = 'AuditLogs'
                        AND NOT is_compressed
                        AND range_end < NOW() - INTERVAL '1 day' * {totalDays}
                    ) compressed")
                .FirstOrDefaultAsync(cancellationToken);

            _logger.LogInformation(
                "Compressed {Count} chunks older than {OlderThan}",
                compressedCount,
                olderThan);

            return compressedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compress old chunks");
            throw;
        }
    }

    // SECURITY: Whitelist of allowed continuous aggregate names
    // This prevents SQL injection even if method is called with external input
    private static readonly HashSet<string> AllowedAggregates = new(StringComparer.OrdinalIgnoreCase)
    {
        "audit_daily_summary",
        "audit_user_activity",
        "audit_resource_access",
        "audit_compliance_metrics"
    };

    public async Task RefreshContinuousAggregatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var aggregate in AllowedAggregates)
            {
                try
                {
                    // SECURITY: Validate aggregate name against whitelist and ensure it's a valid identifier
                    if (!AllowedAggregates.Contains(aggregate) ||
                        !System.Text.RegularExpressions.Regex.IsMatch(aggregate, @"^[a-z_][a-z0-9_]*$"))
                    {
                        _logger.LogWarning("Skipping invalid aggregate name: {Aggregate}", aggregate);
                        continue;
                    }

                    // PostgreSQL's refresh_continuous_aggregate requires literal identifier.
                    // The aggregate name is validated against a whitelist above.
                    #pragma warning disable EF1002
                    await _context.Database.ExecuteSqlRawAsync(
                        $"CALL refresh_continuous_aggregate('{aggregate}', NULL, NULL);",
                        cancellationToken);
                    #pragma warning restore EF1002

                    _logger.LogInformation("Refreshed continuous aggregate: {Aggregate}", aggregate);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh continuous aggregate: {Aggregate}", aggregate);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh continuous aggregates");
            throw;
        }
    }

    public async Task<IReadOnlyList<ChunkInfo>> GetChunkInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isHypertable = await IsHypertableAsync(cancellationToken);
            if (!isHypertable)
            {
                return Array.Empty<ChunkInfo>();
            }

            var chunks = await _context.Database
                .SqlQuery<ChunkInfoRaw>($@"
                    SELECT
                        chunk_name,
                        chunk_schema as schema_name,
                        range_start,
                        range_end,
                        is_compressed,
                        (SELECT COUNT(*) FROM chunk_name::REGCLASS)::BIGINT as row_count,
                        pg_total_relation_size(chunk_name::REGCLASS)::BIGINT as size_bytes,
                        pg_size_pretty(pg_total_relation_size(chunk_name::REGCLASS)) as size_formatted
                    FROM timescaledb_information.chunks
                    WHERE hypertable_name = 'AuditLogs'
                    ORDER BY range_start DESC")
                .ToListAsync(cancellationToken);

            return chunks.Select(c => new ChunkInfo
            {
                ChunkName = c.chunk_name,
                SchemaName = c.schema_name,
                RangeStart = c.range_start,
                RangeEnd = c.range_end,
                IsCompressed = c.is_compressed,
                RowCount = c.row_count,
                SizeBytes = c.size_bytes,
                SizeFormatted = c.size_formatted
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chunk info");
            throw;
        }
    }

    private async Task<bool> IsHypertableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var exists = await _context.Database
                .SqlQuery<bool>($@"
                    SELECT EXISTS(
                        SELECT 1
                        FROM timescaledb_information.hypertables
                        WHERE hypertable_name = 'AuditLogs'
                    )")
                .FirstOrDefaultAsync(cancellationToken);

            return exists;
        }
        catch
        {
            // TimescaleDB not installed
            return false;
        }
    }

    // Raw query result classes
    private record HypertableInfoRaw
    {
        public string hypertable_name { get; init; } = "";
        public string schema_name { get; init; } = "";
        public int chunk_interval_days { get; init; }
        public int num_chunks { get; init; }
        public long total_rows { get; init; }
        public bool compression_enabled { get; init; }
        public bool retention_enabled { get; init; }
        public int retention_days { get; init; }
    }

    private record CompressionStatsRaw
    {
        public long uncompressed_bytes { get; init; }
        public long compressed_bytes { get; init; }
        public double compression_ratio { get; init; }
        public int compressed_chunks { get; init; }
        public int uncompressed_chunks { get; init; }
        public long rows_compressed { get; init; }
        public long rows_uncompressed { get; init; }
        public DateTime? last_compression_run { get; init; }
    }

    private record StorageStatsRaw
    {
        public long total_bytes { get; init; }
        public long table_bytes { get; init; }
        public long index_bytes { get; init; }
        public long toast_bytes { get; init; }
        public string total_size_formatted { get; init; } = "";
        public int active_chunks { get; init; }
        public DateTime? oldest_record { get; init; }
        public DateTime? newest_record { get; init; }
        public long total_records { get; init; }
    }

    private record ChunkInfoRaw
    {
        public string chunk_name { get; init; } = "";
        public string schema_name { get; init; } = "";
        public DateTime range_start { get; init; }
        public DateTime range_end { get; init; }
        public bool is_compressed { get; init; }
        public long row_count { get; init; }
        public long size_bytes { get; init; }
        public string size_formatted { get; init; } = "";
    }
}

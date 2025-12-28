namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Service for querying audit log statistics and compliance metrics
/// Provides efficient access to pre-aggregated data via TimescaleDB continuous aggregates
///
/// HIPAA COMPLIANCE:
/// - Supports compliance officer reporting requirements
/// - Provides user activity monitoring for access audits
/// - Enables PHI access pattern analysis
/// </summary>
public interface IAuditStatisticsService
{
    /// <summary>
    /// Get audit summary for a specific day
    /// </summary>
    Task<DailyAuditSummaryDto> GetDailySummaryAsync(
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit summaries for a date range
    /// </summary>
    Task<IReadOnlyList<DailyAuditSummaryDto>> GetDailySummariesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get activity summary for a specific user
    /// </summary>
    Task<UserActivitySummaryDto> GetUserActivityAsync(
        string userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top active users in a date range
    /// </summary>
    Task<IReadOnlyList<TopUserActivityDto>> GetTopActiveUsersAsync(
        DateTime startDate,
        DateTime endDate,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get access summary for a specific resource
    /// </summary>
    Task<ResourceAccessSummaryDto> GetResourceAccessAsync(
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get HIPAA compliance metrics for a date range
    /// </summary>
    Task<ComplianceMetricsDto> GetComplianceMetricsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get hourly activity trend for a specific day
    /// </summary>
    Task<IReadOnlyList<HourlyActivityTrendDto>> GetHourlyActivityTrendAsync(
        DateTime date,
        CancellationToken cancellationToken = default);
}

// DTOs for audit statistics
public record DailyAuditSummaryDto
{
    public DateTime Date { get; init; }
    public long TotalEvents { get; init; }
    public long SuccessfulEvents { get; init; }
    public long FailedEvents { get; init; }
    public int UniqueUsers { get; init; }
    public double AverageDurationMs { get; init; }
}

public record UserActivitySummaryDto
{
    public string UserId { get; init; } = "";
    public string? Username { get; init; }
    public long TotalActions { get; init; }
    public int ResourceTypesAccessed { get; init; }
    public long ViewCount { get; init; }
    public long CreateCount { get; init; }
    public long UpdateCount { get; init; }
    public long DeleteCount { get; init; }
    public long FailedActions { get; init; }
    public DateTime? FirstActivity { get; init; }
    public DateTime? LastActivity { get; init; }
}

public record TopUserActivityDto
{
    public string UserId { get; init; } = "";
    public string? Username { get; init; }
    public long TotalActions { get; init; }
    public DateTime? LastActivity { get; init; }
}

public record ResourceAccessSummaryDto
{
    public string ResourceType { get; init; } = "";
    public string? ResourceId { get; init; }
    public long TotalAccesses { get; init; }
    public int UniqueUsers { get; init; }
    public long ViewCount { get; init; }
    public long ModificationCount { get; init; }
    public DateTime? LastAccessed { get; init; }
}

public record ComplianceMetricsDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public long TotalAuditEvents { get; init; }
    public long PhiAccessCount { get; init; }
    public long AccessDeniedCount { get; init; }
    public long AuthEventCount { get; init; }
    public long FailedLoginCount { get; init; }
    public long ExportPrintCount { get; init; }
    public int ActiveUsers { get; init; }
    public int UniqueIpAddresses { get; init; }
    public int UniqueSessions { get; init; }
}

public record HourlyActivityTrendDto
{
    public int Hour { get; init; }
    public long EventCount { get; init; }
    public int UniqueUsers { get; init; }
}

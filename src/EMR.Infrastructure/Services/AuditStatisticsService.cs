using EMR.Application.Common.Interfaces;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Services;

/// <summary>
/// Service for querying audit statistics from TimescaleDB continuous aggregates
/// Provides efficient access to pre-aggregated compliance metrics
///
/// HIPAA COMPLIANCE:
/// - Supports compliance officer reporting requirements
/// - Provides user activity monitoring for access audits
/// - Enables PHI access pattern analysis
/// </summary>
public class AuditStatisticsService : IAuditStatisticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditStatisticsService> _logger;

    public AuditStatisticsService(
        ApplicationDbContext context,
        ILogger<AuditStatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DailyAuditSummaryDto> GetDailySummaryAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            // Try to get from continuous aggregate first
            var summary = await TryGetFromAggregateAsync<DailyAuditSummaryRaw>(
                @"SELECT
                    bucket as date,
                    SUM(total_events)::BIGINT as total_events,
                    SUM(successful_events)::BIGINT as successful_events,
                    SUM(failed_events)::BIGINT as failed_events,
                    SUM(unique_users)::INTEGER as unique_users,
                    AVG(avg_duration_ms)::DOUBLE PRECISION as avg_duration_ms
                FROM audit_daily_summary
                WHERE bucket >= {0}::TIMESTAMP
                AND bucket < {1}::TIMESTAMP
                GROUP BY bucket",
                new object[] { startOfDay, endOfDay },
                cancellationToken);

            if (summary != null)
            {
                return new DailyAuditSummaryDto
                {
                    Date = summary.date,
                    TotalEvents = summary.total_events,
                    SuccessfulEvents = summary.successful_events,
                    FailedEvents = summary.failed_events,
                    UniqueUsers = summary.unique_users,
                    AverageDurationMs = summary.avg_duration_ms
                };
            }

            // Fallback to direct query
            var events = await _context.AuditLogs
                .Where(a => a.Timestamp >= startOfDay && a.Timestamp < endOfDay)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalEvents = g.LongCount(),
                    SuccessfulEvents = g.LongCount(a => a.Success),
                    FailedEvents = g.LongCount(a => !a.Success),
                    UniqueUsers = g.Select(a => a.UserId).Distinct().Count(),
                    AvgDuration = g.Average(a => a.DurationMs ?? 0)
                })
                .FirstOrDefaultAsync(cancellationToken);

            return new DailyAuditSummaryDto
            {
                Date = startOfDay,
                TotalEvents = events?.TotalEvents ?? 0,
                SuccessfulEvents = events?.SuccessfulEvents ?? 0,
                FailedEvents = events?.FailedEvents ?? 0,
                UniqueUsers = events?.UniqueUsers ?? 0,
                AverageDurationMs = events?.AvgDuration ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get daily audit summary for {Date}", date);
            throw;
        }
    }

    public async Task<IReadOnlyList<DailyAuditSummaryDto>> GetDailySummariesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            // Try continuous aggregate first
            var summaries = await TryGetListFromAggregateAsync<DailyAuditSummaryRaw>(
                @"SELECT
                    bucket as date,
                    SUM(total_events)::BIGINT as total_events,
                    SUM(successful_events)::BIGINT as successful_events,
                    SUM(failed_events)::BIGINT as failed_events,
                    MAX(unique_users)::INTEGER as unique_users,
                    AVG(avg_duration_ms)::DOUBLE PRECISION as avg_duration_ms
                FROM audit_daily_summary
                WHERE bucket >= {0}::TIMESTAMP
                AND bucket < {1}::TIMESTAMP
                GROUP BY bucket
                ORDER BY bucket",
                new object[] { start, end },
                cancellationToken);

            if (summaries.Count > 0)
            {
                return summaries.Select(s => new DailyAuditSummaryDto
                {
                    Date = s.date,
                    TotalEvents = s.total_events,
                    SuccessfulEvents = s.successful_events,
                    FailedEvents = s.failed_events,
                    UniqueUsers = s.unique_users,
                    AverageDurationMs = s.avg_duration_ms
                }).ToList();
            }

            // Fallback to direct query
            var events = await _context.AuditLogs
                .Where(a => a.Timestamp >= start && a.Timestamp < end)
                .GroupBy(a => a.Timestamp.Date)
                .Select(g => new DailyAuditSummaryDto
                {
                    Date = g.Key,
                    TotalEvents = g.LongCount(),
                    SuccessfulEvents = g.LongCount(a => a.Success),
                    FailedEvents = g.LongCount(a => !a.Success),
                    UniqueUsers = g.Select(a => a.UserId).Distinct().Count(),
                    AverageDurationMs = g.Average(a => a.DurationMs ?? 0)
                })
                .OrderBy(s => s.Date)
                .ToListAsync(cancellationToken);

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get daily summaries from {Start} to {End}", startDate, endDate);
            throw;
        }
    }

    public async Task<UserActivitySummaryDto> GetUserActivityAsync(
        string userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            // Try continuous aggregate
            var activity = await TryGetFromAggregateAsync<UserActivityRaw>(
                @"SELECT
                    {0} as user_id,
                    MAX(""Username"") as username,
                    SUM(total_actions)::BIGINT as total_actions,
                    MAX(resource_types_accessed)::INTEGER as resource_types_accessed,
                    SUM(view_count)::BIGINT as view_count,
                    SUM(create_count)::BIGINT as create_count,
                    SUM(update_count)::BIGINT as update_count,
                    SUM(delete_count)::BIGINT as delete_count,
                    SUM(failed_actions)::BIGINT as failed_actions,
                    MIN(bucket) as first_activity,
                    MAX(bucket) as last_activity
                FROM audit_user_activity
                WHERE ""UserId"" = {0}
                AND bucket >= {1}::TIMESTAMP
                AND bucket < {2}::TIMESTAMP
                GROUP BY ""UserId""",
                new object[] { userId, start, end },
                cancellationToken);

            if (activity != null)
            {
                return new UserActivitySummaryDto
                {
                    UserId = activity.user_id,
                    Username = activity.username,
                    TotalActions = activity.total_actions,
                    ResourceTypesAccessed = activity.resource_types_accessed,
                    ViewCount = activity.view_count,
                    CreateCount = activity.create_count,
                    UpdateCount = activity.update_count,
                    DeleteCount = activity.delete_count,
                    FailedActions = activity.failed_actions,
                    FirstActivity = activity.first_activity,
                    LastActivity = activity.last_activity
                };
            }

            // Fallback to direct query
            var userActivity = await _context.AuditLogs
                .Where(a => a.UserId == userId && a.Timestamp >= start && a.Timestamp < end)
                .GroupBy(a => new { a.UserId, a.Username })
                .Select(g => new UserActivitySummaryDto
                {
                    UserId = g.Key.UserId,
                    Username = g.Key.Username,
                    TotalActions = g.LongCount(),
                    ResourceTypesAccessed = g.Select(a => a.ResourceType).Distinct().Count(),
                    ViewCount = g.LongCount(a => a.EventType == Domain.Enums.AuditEventType.View),
                    CreateCount = g.LongCount(a => a.EventType == Domain.Enums.AuditEventType.Create),
                    UpdateCount = g.LongCount(a => a.EventType == Domain.Enums.AuditEventType.Update),
                    DeleteCount = g.LongCount(a => a.EventType == Domain.Enums.AuditEventType.Delete),
                    FailedActions = g.LongCount(a => !a.Success),
                    FirstActivity = g.Min(a => a.Timestamp),
                    LastActivity = g.Max(a => a.Timestamp)
                })
                .FirstOrDefaultAsync(cancellationToken);

            return userActivity ?? new UserActivitySummaryDto
            {
                UserId = userId,
                Username = null,
                TotalActions = 0,
                ResourceTypesAccessed = 0,
                ViewCount = 0,
                CreateCount = 0,
                UpdateCount = 0,
                DeleteCount = 0,
                FailedActions = 0,
                FirstActivity = null,
                LastActivity = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity for {UserId}", userId);
            throw;
        }
    }

    public async Task<IReadOnlyList<TopUserActivityDto>> GetTopActiveUsersAsync(
        DateTime startDate,
        DateTime endDate,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            var topUsers = await _context.AuditLogs
                .Where(a => a.Timestamp >= start && a.Timestamp < end)
                .GroupBy(a => new { a.UserId, a.Username })
                .Select(g => new TopUserActivityDto
                {
                    UserId = g.Key.UserId,
                    Username = g.Key.Username,
                    TotalActions = g.LongCount(),
                    LastActivity = g.Max(a => a.Timestamp)
                })
                .OrderByDescending(u => u.TotalActions)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return topUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top active users");
            throw;
        }
    }

    public async Task<ResourceAccessSummaryDto> GetResourceAccessAsync(
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try continuous aggregate
            var access = await TryGetFromAggregateAsync<ResourceAccessRaw>(
                @"SELECT
                    {0} as resource_type,
                    {1} as resource_id,
                    SUM(total_accesses)::BIGINT as total_accesses,
                    MAX(unique_users)::INTEGER as unique_users,
                    SUM(view_count)::BIGINT as view_count,
                    SUM(modification_count)::BIGINT as modification_count,
                    MAX(last_accessed) as last_accessed
                FROM audit_resource_access
                WHERE ""ResourceType"" = {0}
                AND ""ResourceId"" = {1}
                GROUP BY ""ResourceType"", ""ResourceId""",
                new object[] { resourceType, resourceId },
                cancellationToken);

            if (access != null)
            {
                return new ResourceAccessSummaryDto
                {
                    ResourceType = access.resource_type,
                    ResourceId = access.resource_id,
                    TotalAccesses = access.total_accesses,
                    UniqueUsers = access.unique_users,
                    ViewCount = access.view_count,
                    ModificationCount = access.modification_count,
                    LastAccessed = access.last_accessed
                };
            }

            // Fallback
            var resourceAccess = await _context.AuditLogs
                .Where(a => a.ResourceType == resourceType && a.ResourceId == resourceId)
                .GroupBy(a => new { a.ResourceType, a.ResourceId })
                .Select(g => new ResourceAccessSummaryDto
                {
                    ResourceType = g.Key.ResourceType,
                    ResourceId = g.Key.ResourceId,
                    TotalAccesses = g.LongCount(),
                    UniqueUsers = g.Select(a => a.UserId).Distinct().Count(),
                    ViewCount = g.LongCount(a => a.EventType == Domain.Enums.AuditEventType.View),
                    ModificationCount = g.LongCount(a => a.EventType == Domain.Enums.AuditEventType.Update),
                    LastAccessed = g.Max(a => a.Timestamp)
                })
                .FirstOrDefaultAsync(cancellationToken);

            return resourceAccess ?? new ResourceAccessSummaryDto
            {
                ResourceType = resourceType,
                ResourceId = resourceId,
                TotalAccesses = 0,
                UniqueUsers = 0,
                ViewCount = 0,
                ModificationCount = 0,
                LastAccessed = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource access for {Type}/{Id}", resourceType, resourceId);
            throw;
        }
    }

    public async Task<ComplianceMetricsDto> GetComplianceMetricsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            // Try continuous aggregate
            var metrics = await TryGetFromAggregateAsync<ComplianceMetricsRaw>(
                @"SELECT
                    SUM(total_audit_events)::BIGINT as total_audit_events,
                    SUM(phi_access_count)::BIGINT as phi_access_count,
                    SUM(access_denied_count)::BIGINT as access_denied_count,
                    SUM(auth_event_count)::BIGINT as auth_event_count,
                    SUM(failed_login_count)::BIGINT as failed_login_count,
                    SUM(export_print_count)::BIGINT as export_print_count,
                    MAX(active_users)::INTEGER as active_users,
                    MAX(unique_ip_addresses)::INTEGER as unique_ip_addresses,
                    MAX(unique_sessions)::INTEGER as unique_sessions
                FROM audit_compliance_metrics
                WHERE bucket >= {0}::TIMESTAMP
                AND bucket < {1}::TIMESTAMP",
                new object[] { start, end },
                cancellationToken);

            if (metrics != null)
            {
                return new ComplianceMetricsDto
                {
                    StartDate = start,
                    EndDate = end.AddDays(-1),
                    TotalAuditEvents = metrics.total_audit_events,
                    PhiAccessCount = metrics.phi_access_count,
                    AccessDeniedCount = metrics.access_denied_count,
                    AuthEventCount = metrics.auth_event_count,
                    FailedLoginCount = metrics.failed_login_count,
                    ExportPrintCount = metrics.export_print_count,
                    ActiveUsers = metrics.active_users,
                    UniqueIpAddresses = metrics.unique_ip_addresses,
                    UniqueSessions = metrics.unique_sessions
                };
            }

            // Fallback to direct query
            var complianceMetrics = await _context.AuditLogs
                .Where(a => a.Timestamp >= start && a.Timestamp < end)
                .GroupBy(_ => 1)
                .Select(g => new ComplianceMetricsDto
                {
                    StartDate = start,
                    EndDate = end.AddDays(-1),
                    TotalAuditEvents = g.LongCount(),
                    PhiAccessCount = g.LongCount(a => a.ResourceType == "Patient"),
                    AccessDeniedCount = g.LongCount(a => a.EventType == Domain.Enums.AuditEventType.AccessDenied),
                    AuthEventCount = g.LongCount(a =>
                        a.EventType == Domain.Enums.AuditEventType.Login ||
                        a.EventType == Domain.Enums.AuditEventType.FailedLogin),
                    FailedLoginCount = g.LongCount(a => a.EventType == Domain.Enums.AuditEventType.FailedLogin),
                    ExportPrintCount = g.LongCount(a =>
                        a.EventType == Domain.Enums.AuditEventType.Export ||
                        a.EventType == Domain.Enums.AuditEventType.Print),
                    ActiveUsers = g.Select(a => a.UserId).Distinct().Count(),
                    UniqueIpAddresses = g.Select(a => a.IpAddress).Distinct().Count(),
                    UniqueSessions = g.Select(a => a.SessionId).Distinct().Count()
                })
                .FirstOrDefaultAsync(cancellationToken);

            return complianceMetrics ?? new ComplianceMetricsDto
            {
                StartDate = start,
                EndDate = end.AddDays(-1),
                TotalAuditEvents = 0,
                PhiAccessCount = 0,
                AccessDeniedCount = 0,
                AuthEventCount = 0,
                FailedLoginCount = 0,
                ExportPrintCount = 0,
                ActiveUsers = 0,
                UniqueIpAddresses = 0,
                UniqueSessions = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get compliance metrics");
            throw;
        }
    }

    public async Task<IReadOnlyList<HourlyActivityTrendDto>> GetHourlyActivityTrendAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            var hourlyData = await _context.AuditLogs
                .Where(a => a.Timestamp >= startOfDay && a.Timestamp < endOfDay)
                .GroupBy(a => a.Timestamp.Hour)
                .Select(g => new HourlyActivityTrendDto
                {
                    Hour = g.Key,
                    EventCount = g.LongCount(),
                    UniqueUsers = g.Select(a => a.UserId).Distinct().Count()
                })
                .OrderBy(h => h.Hour)
                .ToListAsync(cancellationToken);

            // Fill in missing hours
            var result = new List<HourlyActivityTrendDto>();
            for (int hour = 0; hour < 24; hour++)
            {
                var existing = hourlyData.FirstOrDefault(h => h.Hour == hour);
                result.Add(existing ?? new HourlyActivityTrendDto
                {
                    Hour = hour,
                    EventCount = 0,
                    UniqueUsers = 0
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hourly activity trend for {Date}", date);
            throw;
        }
    }

    private async Task<T?> TryGetFromAggregateAsync<T>(
        string sql,
        object[] parameters,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<T>(sql, parameters)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Continuous aggregate query failed, falling back to direct query");
            return null;
        }
    }

    private async Task<IReadOnlyList<T>> TryGetListFromAggregateAsync<T>(
        string sql,
        object[] parameters,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<T>(sql, parameters)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Continuous aggregate query failed, falling back to direct query");
            return Array.Empty<T>();
        }
    }

    // Raw query result classes for TimescaleDB continuous aggregates
    private record DailyAuditSummaryRaw
    {
        public DateTime date { get; init; }
        public long total_events { get; init; }
        public long successful_events { get; init; }
        public long failed_events { get; init; }
        public int unique_users { get; init; }
        public double avg_duration_ms { get; init; }
    }

    private record UserActivityRaw
    {
        public string user_id { get; init; } = "";
        public string? username { get; init; }
        public long total_actions { get; init; }
        public int resource_types_accessed { get; init; }
        public long view_count { get; init; }
        public long create_count { get; init; }
        public long update_count { get; init; }
        public long delete_count { get; init; }
        public long failed_actions { get; init; }
        public DateTime? first_activity { get; init; }
        public DateTime? last_activity { get; init; }
    }

    private record ResourceAccessRaw
    {
        public string resource_type { get; init; } = "";
        public string? resource_id { get; init; }
        public long total_accesses { get; init; }
        public int unique_users { get; init; }
        public long view_count { get; init; }
        public long modification_count { get; init; }
        public DateTime? last_accessed { get; init; }
    }

    private record ComplianceMetricsRaw
    {
        public long total_audit_events { get; init; }
        public long phi_access_count { get; init; }
        public long access_denied_count { get; init; }
        public long auth_event_count { get; init; }
        public long failed_login_count { get; init; }
        public long export_print_count { get; init; }
        public int active_users { get; init; }
        public int unique_ip_addresses { get; init; }
        public int unique_sessions { get; init; }
    }
}

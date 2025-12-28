using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Features.Audit.DTOs;
using EMR.Application.Features.Audit.Queries.GetAuditLogs;
using EMR.Application.Features.Audit.Queries.GetResourceAuditTrail;
using EMR.Infrastructure.TimescaleDb;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace EMR.Api.Controllers;

/// <summary>
/// Controller for HIPAA audit log management and compliance reporting
/// ADMIN-ONLY ACCESS - These endpoints are restricted to compliance officers and system administrators
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // CRITICAL: Only Admin role can access audit logs
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuditController> _logger;
    private readonly IAuditStatisticsService _statisticsService;
    private readonly ITimescaleDbConfiguration _timescaleDbConfig;

    public AuditController(
        IMediator mediator,
        ILogger<AuditController> logger,
        IAuditStatisticsService statisticsService,
        ITimescaleDbConfiguration timescaleDbConfig)
    {
        _mediator = mediator;
        _logger = logger;
        _statisticsService = statisticsService;
        _timescaleDbConfig = timescaleDbConfig;
    }

    /// <summary>
    /// Query audit logs with filtering and pagination
    /// </summary>
    /// <remarks>
    /// Allows compliance officers to search audit logs for HIPAA compliance review.
    ///
    /// Sample request:
    ///
    ///     GET /api/audit?userId=123&amp;eventType=View&amp;fromDate=2024-01-01&amp;pageSize=50
    ///
    /// </remarks>
    /// <param name="query">Query parameters for filtering audit logs</param>
    /// <returns>Paginated list of audit log entries</returns>
    /// <response code="200">Returns the paginated audit logs</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="403">Forbidden - user does not have Admin role</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryDto query)
    {
        try
        {
            var command = new GetAuditLogsQuery { QueryParams = query };
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, new { message = "An error occurred while retrieving audit logs" });
        }
    }

    /// <summary>
    /// Get complete audit trail for a specific resource
    /// </summary>
    /// <remarks>
    /// Retrieves all audit events for a specific resource (e.g., Patient, Encounter).
    /// Shows who accessed or modified the resource and when.
    ///
    /// Sample request:
    ///
    ///     GET /api/audit/trail/Patient/123e4567-e89b-12d3-a456-426614174000
    ///
    /// </remarks>
    /// <param name="resourceType">Type of resource (e.g., Patient, Encounter)</param>
    /// <param name="resourceId">Unique identifier of the resource</param>
    /// <returns>Complete audit trail for the resource</returns>
    /// <response code="200">Returns the audit trail</response>
    /// <response code="400">Bad request - invalid parameters</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="403">Forbidden - user does not have Admin role</response>
    [HttpGet("trail/{resourceType}/{resourceId}")]
    [ProducesResponseType(typeof(ResultDto<IEnumerable<AuditLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetResourceAuditTrail(
        [FromRoute] string resourceType,
        [FromRoute] string resourceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return BadRequest(new { message = "Resource type is required" });

            if (string.IsNullOrWhiteSpace(resourceId))
                return BadRequest(new { message = "Resource ID is required" });

            var query = new GetResourceAuditTrailQuery
            {
                ResourceType = resourceType,
                ResourceId = resourceId
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for {ResourceType}/{ResourceId}",
                resourceType, resourceId);
            return StatusCode(500, new { message = "An error occurred while retrieving audit trail" });
        }
    }

    /// <summary>
    /// Get HIPAA compliance metrics for a date range
    /// </summary>
    /// <remarks>
    /// Provides HIPAA compliance metrics using TimescaleDB continuous aggregates for optimal performance.
    /// Includes PHI access counts, authentication events, and access denied incidents.
    ///
    /// Sample request:
    ///
    ///     GET /api/audit/compliance/metrics?fromDate=2024-01-01&amp;toDate=2024-01-31
    ///
    /// </remarks>
    /// <param name="fromDate">Start date for metrics (UTC)</param>
    /// <param name="toDate">End date for metrics (UTC)</param>
    /// <returns>HIPAA compliance metrics</returns>
    /// <response code="200">Returns compliance metrics</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="403">Forbidden - user does not have Admin role</response>
    [HttpGet("compliance/metrics")]
    [ProducesResponseType(typeof(ComplianceMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetComplianceMetrics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            // Default to last 30 days if not specified
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var metrics = await _statisticsService.GetComplianceMetricsAsync(from, to);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving compliance metrics");
            return StatusCode(500, new { message = "An error occurred while retrieving compliance metrics" });
        }
    }

    /// <summary>
    /// Get daily audit summaries for a date range
    /// </summary>
    /// <remarks>
    /// Returns aggregated daily audit statistics using TimescaleDB continuous aggregates.
    /// Useful for trend analysis and compliance dashboards.
    ///
    /// Sample request:
    ///
    ///     GET /api/audit/daily-summaries?fromDate=2024-01-01&amp;toDate=2024-01-31
    ///
    /// </remarks>
    [HttpGet("daily-summaries")]
    [ProducesResponseType(typeof(IEnumerable<DailyAuditSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDailySummaries(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var summaries = await _statisticsService.GetDailySummariesAsync(from, to);
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily summaries");
            return StatusCode(500, new { message = "An error occurred while retrieving daily summaries" });
        }
    }

    /// <summary>
    /// Get hourly activity trend for a specific day
    /// </summary>
    /// <remarks>
    /// Returns hour-by-hour activity breakdown for the specified date.
    /// Useful for identifying peak usage times and detecting anomalies.
    /// </remarks>
    [HttpGet("hourly-trend")]
    [ProducesResponseType(typeof(IEnumerable<HourlyActivityTrendDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHourlyActivityTrend([FromQuery] DateTime? date)
    {
        try
        {
            var targetDate = date ?? DateTime.UtcNow.Date;
            var trend = await _statisticsService.GetHourlyActivityTrendAsync(targetDate);
            return Ok(trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hourly trend");
            return StatusCode(500, new { message = "An error occurred while retrieving hourly trend" });
        }
    }

    /// <summary>
    /// Get user activity summary
    /// </summary>
    /// <remarks>
    /// Returns detailed activity summary for a specific user over a date range.
    /// Includes action counts by type and resource access patterns.
    /// </remarks>
    [HttpGet("users/{userId}/activity")]
    [ProducesResponseType(typeof(UserActivitySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserActivity(
        [FromRoute] string userId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var activity = await _statisticsService.GetUserActivityAsync(userId, from, to);
            return Ok(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activity for {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving user activity" });
        }
    }

    /// <summary>
    /// Get top active users
    /// </summary>
    /// <remarks>
    /// Returns list of most active users by total actions in the specified date range.
    /// Useful for identifying high-activity accounts for compliance review.
    /// </remarks>
    [HttpGet("users/top-active")]
    [ProducesResponseType(typeof(IEnumerable<TopUserActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTopActiveUsers(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int limit = 10)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var topUsers = await _statisticsService.GetTopActiveUsersAsync(from, to, limit);
            return Ok(topUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top active users");
            return StatusCode(500, new { message = "An error occurred while retrieving top active users" });
        }
    }

    /// <summary>
    /// Get resource access summary
    /// </summary>
    /// <remarks>
    /// Returns access pattern summary for a specific resource.
    /// Shows unique users, access counts, and modification counts.
    /// </remarks>
    [HttpGet("resources/{resourceType}/{resourceId}/access")]
    [ProducesResponseType(typeof(ResourceAccessSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetResourceAccess(
        [FromRoute] string resourceType,
        [FromRoute] string resourceId)
    {
        try
        {
            var access = await _statisticsService.GetResourceAccessAsync(resourceType, resourceId);
            return Ok(access);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resource access for {Type}/{Id}", resourceType, resourceId);
            return StatusCode(500, new { message = "An error occurred while retrieving resource access" });
        }
    }

    /// <summary>
    /// Get TimescaleDB storage statistics
    /// </summary>
    /// <remarks>
    /// Returns storage statistics for the AuditLogs hypertable including compression ratio,
    /// chunk information, and retention policy status.
    /// </remarks>
    [HttpGet("storage/stats")]
    [ProducesResponseType(typeof(StorageStats), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStorageStats()
    {
        try
        {
            var storageStats = await _timescaleDbConfig.GetStorageStatsAsync();
            var compressionStats = await _timescaleDbConfig.GetCompressionStatsAsync();
            var hypertableInfo = await _timescaleDbConfig.GetHypertableInfoAsync();

            return Ok(new
            {
                Storage = storageStats,
                Compression = compressionStats,
                Hypertable = hypertableInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage stats");
            return StatusCode(500, new { message = "An error occurred while retrieving storage stats" });
        }
    }

    /// <summary>
    /// Check HIPAA retention compliance status
    /// </summary>
    /// <remarks>
    /// Validates that audit log retention meets HIPAA 7-year requirement.
    /// Returns compliance status and next retention policy execution time.
    /// </remarks>
    [HttpGet("compliance/retention")]
    [ProducesResponseType(typeof(RetentionComplianceStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRetentionCompliance()
    {
        try
        {
            var status = await _timescaleDbConfig.CheckRetentionComplianceAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking retention compliance");
            return StatusCode(500, new { message = "An error occurred while checking retention compliance" });
        }
    }

    /// <summary>
    /// Get TimescaleDB chunk information
    /// </summary>
    /// <remarks>
    /// Returns information about all chunks in the AuditLogs hypertable.
    /// Useful for monitoring storage distribution and compression status.
    /// </remarks>
    [HttpGet("storage/chunks")]
    [ProducesResponseType(typeof(IEnumerable<ChunkInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChunkInfo()
    {
        try
        {
            var chunks = await _timescaleDbConfig.GetChunkInfoAsync();
            return Ok(chunks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chunk info");
            return StatusCode(500, new { message = "An error occurred while retrieving chunk info" });
        }
    }

    /// <summary>
    /// Refresh TimescaleDB continuous aggregates
    /// </summary>
    /// <remarks>
    /// Manually triggers refresh of all continuous aggregates for up-to-date compliance metrics.
    /// Normally aggregates refresh automatically on their configured schedule.
    /// </remarks>
    [HttpPost("maintenance/refresh-aggregates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RefreshContinuousAggregates()
    {
        try
        {
            _logger.LogInformation("Manual refresh of continuous aggregates triggered by {User}",
                User.Identity?.Name);

            await _timescaleDbConfig.RefreshContinuousAggregatesAsync();

            return Ok(new { message = "Continuous aggregates refresh completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing continuous aggregates");
            return StatusCode(500, new { message = "An error occurred while refreshing aggregates" });
        }
    }

    /// <summary>
    /// Get audit statistics for compliance reporting (legacy endpoint)
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ComplianceMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        // Redirect to new compliance metrics endpoint
        return await GetComplianceMetrics(fromDate, toDate);
    }

    /// <summary>
    /// Export audit logs to file for compliance archival (streaming)
    /// </summary>
    /// <remarks>
    /// Exports audit logs to CSV or JSON format for long-term archival.
    /// Uses streaming response for large datasets to avoid memory issues.
    /// Required for HIPAA 7-year retention compliance.
    ///
    /// Sample request:
    ///
    ///     GET /api/audit/export/stream?fromDate=2024-01-01&amp;toDate=2024-12-31&amp;format=csv
    ///
    /// </remarks>
    /// <param name="fromDate">Start date for export</param>
    /// <param name="toDate">End date for export</param>
    /// <param name="format">Export format (csv or json)</param>
    /// <returns>Streaming file download of audit logs</returns>
    /// <response code="200">Returns the audit log file stream</response>
    /// <response code="400">Bad request - invalid parameters</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="403">Forbidden - user does not have Admin role</response>
    [HttpGet("export/stream")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAuditLogsStream(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string format = "csv")
    {
        try
        {
            if (fromDate > toDate)
                return BadRequest(new { message = "FromDate must be before ToDate" });

            _logger.LogInformation(
                "AUDIT_EXPORT_STREAM | User: {UserId} | FromDate: {FromDate} | ToDate: {ToDate} | Format: {Format}",
                User.Identity?.Name, fromDate, toDate, format);

            // Get audit logs for the date range
            var query = new AuditLogQueryDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                PageSize = 10000 // Large page for streaming
            };

            var command = new GetAuditLogsQuery { QueryParams = query };
            var result = await _mediator.Send(command);

            if (result.Items == null || result.Items.Count == 0)
                return NotFound(new { message = "No audit logs found for the specified date range" });

            var logs = result.Items;
            var fileName = $"audit_logs_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}";

            if (format.ToLower() == "json")
            {
                var jsonContent = JsonSerializer.Serialize(logs, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                var bytes = Encoding.UTF8.GetBytes(jsonContent);
                return File(bytes, "application/json", $"{fileName}.json");
            }
            else
            {
                // CSV format
                var csv = new StringBuilder();
                csv.AppendLine("Id,EventType,UserId,Username,Timestamp,ResourceType,ResourceId,Action,Success,IpAddress,SessionId,CorrelationId");

                foreach (var log in logs)
                {
                    csv.AppendLine($"\"{log.Id}\",\"{log.EventTypeName}\",\"{log.UserId}\",\"{EscapeCsv(log.Username)}\",\"{log.Timestamp:O}\",\"{log.ResourceType}\",\"{log.ResourceId}\",\"{EscapeCsv(log.Action)}\",\"{log.Success}\",\"{log.IpAddress}\",\"{log.SessionId}\",\"{log.CorrelationId}\"");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"{fileName}.csv");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(500, new { message = "An error occurred while exporting audit logs" });
        }
    }

    /// <summary>
    /// Export audit logs to file for compliance archival (legacy endpoint)
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAuditLogs([FromBody] AuditExportRequest request)
    {
        return await ExportAuditLogsStream(request.FromDate, request.ToDate, request.Format);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\"", "\"\"");
    }
}

/// <summary>
/// Request model for audit log export
/// </summary>
public class AuditExportRequest
{
    /// <summary>
    /// Start date for export (UTC)
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// End date for export (UTC)
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Export format (csv or json)
    /// </summary>
    public string Format { get; set; } = "csv";
}

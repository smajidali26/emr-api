using EMR.Domain.Enums;

namespace EMR.Application.Features.Audit.DTOs;

/// <summary>
/// Query parameters for filtering audit logs
/// Supports HIPAA compliance officer requirements for audit trail review
/// </summary>
public sealed class AuditLogQueryDto
{
    /// <summary>
    /// Filter by user ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by username (partial match)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Filter by event type
    /// </summary>
    public AuditEventType? EventType { get; set; }

    /// <summary>
    /// Filter by resource type (e.g., "Patient", "Encounter")
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Filter by specific resource ID
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Filter by IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Filter by action (partial match)
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Filter by success status
    /// </summary>
    public bool? Success { get; set; }

    /// <summary>
    /// Filter from date (UTC)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter to date (UTC)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter by HTTP method
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Filter by session ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Filter by correlation ID
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size (max 1000)
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort by field (default: Timestamp)
    /// </summary>
    public string SortBy { get; set; } = "Timestamp";

    /// <summary>
    /// Sort direction (true = descending, false = ascending)
    /// </summary>
    public bool SortDescending { get; set; } = true;

    /// <summary>
    /// Validate and sanitize query parameters
    /// </summary>
    public void Validate()
    {
        // Ensure page number is at least 1
        if (PageNumber < 1)
            PageNumber = 1;

        // Limit page size to prevent excessive data retrieval
        if (PageSize < 1)
            PageSize = 50;
        if (PageSize > 1000)
            PageSize = 1000;

        // Validate date range
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            // Swap dates if reversed
            (FromDate, ToDate) = (ToDate, FromDate);
        }

        // Ensure we don't query too far back (HIPAA requires 6 years retention)
        // But limit UI queries to prevent performance issues
        if (!FromDate.HasValue && !ToDate.HasValue)
        {
            // Default to last 30 days if no date range specified
            FromDate = DateTime.UtcNow.AddDays(-30);
        }
    }
}

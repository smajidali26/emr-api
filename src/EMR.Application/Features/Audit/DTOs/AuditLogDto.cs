using EMR.Domain.Enums;

namespace EMR.Application.Features.Audit.DTOs;

/// <summary>
/// Data transfer object for audit log records
/// Used for API responses and reporting
/// </summary>
public sealed class AuditLogDto
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of audit event
    /// </summary>
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// Display name for event type
    /// </summary>
    public string EventTypeName { get; set; } = string.Empty;

    /// <summary>
    /// User ID who performed the action
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username or email
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Timestamp when the action occurred (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Type of resource accessed
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Specific resource identifier
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// IP address of the user
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Action performed
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the action
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if action failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// HTTP method
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Request path
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Session ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Old values (for modification events) - masked PHI
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values (for modification events) - masked PHI
    /// </summary>
    public string? NewValues { get; set; }
}

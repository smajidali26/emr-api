using EMR.Domain.Enums;

namespace EMR.Application.Features.Audit.DTOs;

/// <summary>
/// DTO for creating a new audit log entry
/// </summary>
public sealed class CreateAuditLogDto
{
    /// <summary>
    /// Type of audit event
    /// </summary>
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// User ID who performed the action
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username or email
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Type of resource accessed
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Specific resource identifier
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Action performed
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the user
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Additional details about the action
    /// </summary>
    public string? Details { get; set; }

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
    /// Correlation ID
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Old values (must be sanitized before setting)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values (must be sanitized before setting)
    /// </summary>
    public string? NewValues { get; set; }
}

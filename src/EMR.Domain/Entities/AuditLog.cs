using EMR.Domain.Enums;

namespace EMR.Domain.Entities;

/// <summary>
/// HIPAA-compliant audit log entity
/// Tracks WHO accessed WHAT, WHEN, from WHERE for compliance and security
/// Audit logs are immutable - no updates or deletes allowed
/// </summary>
public sealed class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Type of audit event (View, Create, Update, Delete, etc.)
    /// </summary>
    public AuditEventType EventType { get; private set; }

    /// <summary>
    /// User ID who performed the action (WHO)
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// Username or email for easier audit trail review
    /// </summary>
    public string? Username { get; private set; }

    /// <summary>
    /// Timestamp when the action occurred (WHEN) - UTC
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Type of resource accessed (WHAT) - e.g., "Patient", "Encounter", "MedicalNote"
    /// </summary>
    public string ResourceType { get; private set; } = string.Empty;

    /// <summary>
    /// Specific resource identifier (WHAT) - e.g., PatientId, EncounterId
    /// </summary>
    public string? ResourceId { get; private set; }

    /// <summary>
    /// IP address of the user (WHERE)
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent string (browser/device information)
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Action performed - descriptive text (e.g., "Viewed patient record", "Updated diagnosis")
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>
    /// Additional details or context about the action
    /// PHI must be masked in this field
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// Error message if action failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// HTTP method for API requests (GET, POST, PUT, DELETE)
    /// </summary>
    public string? HttpMethod { get; private set; }

    /// <summary>
    /// Request path/endpoint
    /// </summary>
    public string? RequestPath { get; private set; }

    /// <summary>
    /// HTTP response status code
    /// </summary>
    public int? StatusCode { get; private set; }

    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    public long? DurationMs { get; private set; }

    /// <summary>
    /// Session ID for correlating related audit events
    /// </summary>
    public string? SessionId { get; private set; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// For data modification events - captures old values (JSON)
    /// PHI must be masked
    /// </summary>
    public string? OldValues { get; private set; }

    /// <summary>
    /// For data modification events - captures new values (JSON)
    /// PHI must be masked
    /// </summary>
    public string? NewValues { get; private set; }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private AuditLog()
    {
    }

    /// <summary>
    /// Create a new audit log entry
    /// </summary>
    public AuditLog(
        AuditEventType eventType,
        string userId,
        string action,
        string resourceType,
        string? resourceId = null,
        string? ipAddress = null,
        string? userAgent = null,
        bool success = true,
        string? details = null,
        string? username = null)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Username = username;
        Timestamp = DateTime.UtcNow;
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        ResourceId = resourceId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Details = details;
        Success = success;
    }

    /// <summary>
    /// Set HTTP request details
    /// </summary>
    public void SetHttpDetails(string httpMethod, string requestPath, int statusCode, long? durationMs = null)
    {
        HttpMethod = httpMethod;
        RequestPath = requestPath;
        StatusCode = statusCode;
        DurationMs = durationMs;
    }

    /// <summary>
    /// Set error information for failed operations
    /// </summary>
    public void SetError(string errorMessage)
    {
        Success = false;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Set tracking identifiers for correlation
    /// </summary>
    public void SetTrackingIds(string? sessionId, string? correlationId)
    {
        SessionId = sessionId;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Set change tracking values (for Update/Delete events)
    /// IMPORTANT: Values must be sanitized to mask PHI before calling this method
    /// </summary>
    public void SetChangeValues(string? oldValues, string? newValues)
    {
        OldValues = oldValues;
        NewValues = newValues;
    }
}

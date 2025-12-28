using EMR.Application.Features.Audit.DTOs;
using EMR.Domain.Enums;

namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Service for creating and managing HIPAA-compliant audit logs
/// Provides structured audit trail for WHO accessed WHAT, WHEN, from WHERE
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Create a new audit log entry
    /// </summary>
    Task<Guid> CreateAuditLogAsync(
        AuditEventType eventType,
        string userId,
        string action,
        string resourceType,
        string? resourceId = null,
        string? ipAddress = null,
        string? userAgent = null,
        bool success = true,
        string? details = null,
        string? username = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an audit log for PHI access (View operations)
    /// </summary>
    Task<Guid> LogPhiAccessAsync(
        string userId,
        string resourceType,
        string resourceId,
        string action,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an audit log for data modification (Create, Update, Delete)
    /// </summary>
    Task<Guid> LogDataModificationAsync(
        AuditEventType eventType,
        string userId,
        string resourceType,
        string resourceId,
        string action,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an audit log for authentication events
    /// </summary>
    Task<Guid> LogAuthenticationAsync(
        AuditEventType eventType,
        string userId,
        string username,
        bool success,
        string? ipAddress = null,
        string? userAgent = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an audit log for failed access attempts
    /// </summary>
    Task<Guid> LogAccessDeniedAsync(
        string userId,
        string resourceType,
        string? resourceId,
        string action,
        string reason,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an audit log for export/print operations
    /// </summary>
    Task<Guid> LogExportOperationAsync(
        AuditEventType eventType,
        string userId,
        string resourceType,
        string? resourceId,
        string format,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an audit log with full HTTP request details
    /// </summary>
    Task<Guid> LogHttpRequestAsync(
        AuditEventType eventType,
        string userId,
        string action,
        string resourceType,
        string? resourceId,
        string httpMethod,
        string requestPath,
        int statusCode,
        long durationMs,
        string? ipAddress = null,
        string? userAgent = null,
        string? sessionId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query audit logs with filters and pagination
    /// </summary>
    Task<(IEnumerable<AuditLogDto> Logs, int TotalCount)> QueryAuditLogsAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit log by ID
    /// </summary>
    Task<AuditLogDto?> GetAuditLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit trail for a specific resource
    /// </summary>
    Task<IEnumerable<AuditLogDto>> GetResourceAuditTrailAsync(
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's access history
    /// </summary>
    Task<IEnumerable<AuditLogDto>> GetUserAuditHistoryAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
}

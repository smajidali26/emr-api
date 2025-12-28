using EMR.Application.Common.Interfaces;
using EMR.Application.Features.Audit.DTOs;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Domain.Interfaces;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Services;

/// <summary>
/// HIPAA-compliant audit service implementation
/// Creates immutable audit logs for compliance and security tracking
/// SECURITY FIX: Task #5 - Fix Unit of Work usage (Kevin White - 6h)
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<AuditService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateAuditLogAsync(
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog(
                eventType: eventType,
                userId: userId,
                action: action,
                resourceType: resourceType,
                resourceId: resourceId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: success,
                details: details,
                username: username);

            _context.AuditLogs.Add(auditLog);

            // SECURITY FIX: Task #5 - Fix Unit of Work usage (Kevin White - 6h)
            // Use IUnitOfWork instead of direct SaveChangesAsync for consistent transaction management
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Also log to Serilog for immediate shipping to SIEM
            LogToSerilog(auditLog);

            return auditLog.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for {EventType} on {ResourceType}/{ResourceId}",
                eventType, resourceType, resourceId);
            throw;
        }
    }

    public async Task<Guid> LogPhiAccessAsync(
        string userId,
        string resourceType,
        string resourceId,
        string action,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        return await CreateAuditLogAsync(
            eventType: AuditEventType.View,
            userId: userId,
            action: action,
            resourceType: resourceType,
            resourceId: resourceId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            success: true,
            details: details,
            cancellationToken: cancellationToken);
    }

    public async Task<Guid> LogDataModificationAsync(
        AuditEventType eventType,
        string userId,
        string resourceType,
        string resourceId,
        string action,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog(
                eventType: eventType,
                userId: userId,
                action: action,
                resourceType: resourceType,
                resourceId: resourceId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: true);

            // Set change values (must be sanitized before calling this)
            auditLog.SetChangeValues(oldValues, newValues);

            _context.AuditLogs.Add(auditLog);

            // SECURITY FIX: Task #5 - Fix Unit of Work usage (Kevin White - 6h)
            // Use IUnitOfWork instead of direct SaveChangesAsync for consistent transaction management
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            LogToSerilog(auditLog);

            return auditLog.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log data modification for {ResourceType}/{ResourceId}",
                resourceType, resourceId);
            throw;
        }
    }

    public async Task<Guid> LogAuthenticationAsync(
        AuditEventType eventType,
        string userId,
        string username,
        bool success,
        string? ipAddress = null,
        string? userAgent = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var action = eventType switch
            {
                AuditEventType.Login => "User login",
                AuditEventType.Logout => "User logout",
                AuditEventType.FailedLogin => "Failed login attempt",
                _ => "Authentication event"
            };

            var auditLog = new AuditLog(
                eventType: eventType,
                userId: userId,
                action: action,
                resourceType: "Authentication",
                resourceId: null,
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: success,
                details: errorMessage,
                username: username);

            if (!success && errorMessage != null)
            {
                auditLog.SetError(errorMessage);
            }

            _context.AuditLogs.Add(auditLog);

            // SECURITY FIX: Task #5 - Fix Unit of Work usage (Kevin White - 6h)
            // Use IUnitOfWork instead of direct SaveChangesAsync for consistent transaction management
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            LogToSerilog(auditLog);

            return auditLog.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log authentication event for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Guid> LogAccessDeniedAsync(
        string userId,
        string resourceType,
        string? resourceId,
        string action,
        string reason,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog(
                eventType: AuditEventType.AccessDenied,
                userId: userId,
                action: action,
                resourceType: resourceType,
                resourceId: resourceId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: false,
                details: $"Access denied: {reason}");

            auditLog.SetError(reason);

            _context.AuditLogs.Add(auditLog);

            // SECURITY FIX: Task #5 - Fix Unit of Work usage (Kevin White - 6h)
            // Use IUnitOfWork instead of direct SaveChangesAsync for consistent transaction management
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            LogToSerilog(auditLog);

            return auditLog.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log access denied for {ResourceType}/{ResourceId}",
                resourceType, resourceId);
            throw;
        }
    }

    public async Task<Guid> LogExportOperationAsync(
        AuditEventType eventType,
        string userId,
        string resourceType,
        string? resourceId,
        string format,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var action = eventType == AuditEventType.Export
            ? $"Exported {resourceType} to {format}"
            : $"Printed {resourceType}";

        return await CreateAuditLogAsync(
            eventType: eventType,
            userId: userId,
            action: action,
            resourceType: resourceType,
            resourceId: resourceId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            success: true,
            details: $"Format: {format}",
            cancellationToken: cancellationToken);
    }

    public async Task<Guid> LogHttpRequestAsync(
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog(
                eventType: eventType,
                userId: userId,
                action: action,
                resourceType: resourceType,
                resourceId: resourceId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: statusCode >= 200 && statusCode < 400);

            auditLog.SetHttpDetails(httpMethod, requestPath, statusCode, durationMs);
            auditLog.SetTrackingIds(sessionId, correlationId);

            _context.AuditLogs.Add(auditLog);

            // SECURITY FIX: Task #5 - Fix Unit of Work usage (Kevin White - 6h)
            // Use IUnitOfWork instead of direct SaveChangesAsync for consistent transaction management
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            LogToSerilog(auditLog);

            return auditLog.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log HTTP request for {RequestPath}", requestPath);
            throw;
        }
    }

    public async Task<(IEnumerable<AuditLogDto> Logs, int TotalCount)> QueryAuditLogsAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryable = _context.AuditLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                queryable = queryable.Where(a => a.UserId == query.UserId);
            }

            if (!string.IsNullOrWhiteSpace(query.Username))
            {
                queryable = queryable.Where(a => a.Username != null && a.Username.Contains(query.Username));
            }

            if (query.EventType.HasValue)
            {
                queryable = queryable.Where(a => a.EventType == query.EventType.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.ResourceType))
            {
                queryable = queryable.Where(a => a.ResourceType == query.ResourceType);
            }

            if (!string.IsNullOrWhiteSpace(query.ResourceId))
            {
                queryable = queryable.Where(a => a.ResourceId == query.ResourceId);
            }

            if (!string.IsNullOrWhiteSpace(query.IpAddress))
            {
                queryable = queryable.Where(a => a.IpAddress == query.IpAddress);
            }

            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                queryable = queryable.Where(a => a.Action.Contains(query.Action));
            }

            if (query.Success.HasValue)
            {
                queryable = queryable.Where(a => a.Success == query.Success.Value);
            }

            if (query.FromDate.HasValue)
            {
                queryable = queryable.Where(a => a.Timestamp >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                queryable = queryable.Where(a => a.Timestamp <= query.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.HttpMethod))
            {
                queryable = queryable.Where(a => a.HttpMethod == query.HttpMethod);
            }

            if (!string.IsNullOrWhiteSpace(query.SessionId))
            {
                queryable = queryable.Where(a => a.SessionId == query.SessionId);
            }

            if (!string.IsNullOrWhiteSpace(query.CorrelationId))
            {
                queryable = queryable.Where(a => a.CorrelationId == query.CorrelationId);
            }

            // Get total count before pagination
            var totalCount = await queryable.CountAsync(cancellationToken);

            // Apply sorting
            queryable = query.SortBy.ToLower() switch
            {
                "eventype" => query.SortDescending
                    ? queryable.OrderByDescending(a => a.EventType)
                    : queryable.OrderBy(a => a.EventType),
                "userid" => query.SortDescending
                    ? queryable.OrderByDescending(a => a.UserId)
                    : queryable.OrderBy(a => a.UserId),
                "resourcetype" => query.SortDescending
                    ? queryable.OrderByDescending(a => a.ResourceType)
                    : queryable.OrderBy(a => a.ResourceType),
                "action" => query.SortDescending
                    ? queryable.OrderByDescending(a => a.Action)
                    : queryable.OrderBy(a => a.Action),
                _ => query.SortDescending
                    ? queryable.OrderByDescending(a => a.Timestamp)
                    : queryable.OrderBy(a => a.Timestamp)
            };

            // Apply pagination
            var logs = await queryable
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    EventType = a.EventType,
                    EventTypeName = a.EventType.ToString(),
                    UserId = a.UserId,
                    Username = a.Username,
                    Timestamp = a.Timestamp,
                    ResourceType = a.ResourceType,
                    ResourceId = a.ResourceId,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    Action = a.Action,
                    Details = a.Details,
                    Success = a.Success,
                    ErrorMessage = a.ErrorMessage,
                    HttpMethod = a.HttpMethod,
                    RequestPath = a.RequestPath,
                    StatusCode = a.StatusCode,
                    DurationMs = a.DurationMs,
                    SessionId = a.SessionId,
                    CorrelationId = a.CorrelationId,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues
                })
                .ToListAsync(cancellationToken);

            return (logs, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query audit logs");
            throw;
        }
    }

    public async Task<AuditLogDto?> GetAuditLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = await _context.AuditLogs
                .Where(a => a.Id == id)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    EventType = a.EventType,
                    EventTypeName = a.EventType.ToString(),
                    UserId = a.UserId,
                    Username = a.Username,
                    Timestamp = a.Timestamp,
                    ResourceType = a.ResourceType,
                    ResourceId = a.ResourceId,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    Action = a.Action,
                    Details = a.Details,
                    Success = a.Success,
                    ErrorMessage = a.ErrorMessage,
                    HttpMethod = a.HttpMethod,
                    RequestPath = a.RequestPath,
                    StatusCode = a.StatusCode,
                    DurationMs = a.DurationMs,
                    SessionId = a.SessionId,
                    CorrelationId = a.CorrelationId,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues
                })
                .FirstOrDefaultAsync(cancellationToken);

            return auditLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit log {AuditLogId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetResourceAuditTrailAsync(
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditTrail = await _context.AuditLogs
                .Where(a => a.ResourceType == resourceType && a.ResourceId == resourceId)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    EventType = a.EventType,
                    EventTypeName = a.EventType.ToString(),
                    UserId = a.UserId,
                    Username = a.Username,
                    Timestamp = a.Timestamp,
                    ResourceType = a.ResourceType,
                    ResourceId = a.ResourceId,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    Action = a.Action,
                    Details = a.Details,
                    Success = a.Success,
                    ErrorMessage = a.ErrorMessage,
                    HttpMethod = a.HttpMethod,
                    RequestPath = a.RequestPath,
                    StatusCode = a.StatusCode,
                    DurationMs = a.DurationMs,
                    SessionId = a.SessionId,
                    CorrelationId = a.CorrelationId,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues
                })
                .ToListAsync(cancellationToken);

            return auditTrail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit trail for {ResourceType}/{ResourceId}",
                resourceType, resourceId);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetUserAuditHistoryAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryable = _context.AuditLogs
                .Where(a => a.UserId == userId);

            if (fromDate.HasValue)
            {
                queryable = queryable.Where(a => a.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                queryable = queryable.Where(a => a.Timestamp <= toDate.Value);
            }

            var history = await queryable
                .OrderByDescending(a => a.Timestamp)
                .Take(pageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    EventType = a.EventType,
                    EventTypeName = a.EventType.ToString(),
                    UserId = a.UserId,
                    Username = a.Username,
                    Timestamp = a.Timestamp,
                    ResourceType = a.ResourceType,
                    ResourceId = a.ResourceId,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    Action = a.Action,
                    Details = a.Details,
                    Success = a.Success,
                    ErrorMessage = a.ErrorMessage,
                    HttpMethod = a.HttpMethod,
                    RequestPath = a.RequestPath,
                    StatusCode = a.StatusCode,
                    DurationMs = a.DurationMs,
                    SessionId = a.SessionId,
                    CorrelationId = a.CorrelationId,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues
                })
                .ToListAsync(cancellationToken);

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit history for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Log audit entry to Serilog for immediate SIEM shipping
    /// </summary>
    private void LogToSerilog(AuditLog auditLog)
    {
        _logger.LogInformation(
            "AUDIT | EventType: {EventType} | User: {UserId} | Resource: {ResourceType}/{ResourceId} | " +
            "Action: {Action} | IP: {IpAddress} | Success: {Success} | Timestamp: {Timestamp}",
            auditLog.EventType,
            auditLog.UserId,
            auditLog.ResourceType,
            auditLog.ResourceId ?? "N/A",
            auditLog.Action,
            auditLog.IpAddress ?? "Unknown",
            auditLog.Success,
            auditLog.Timestamp);
    }
}

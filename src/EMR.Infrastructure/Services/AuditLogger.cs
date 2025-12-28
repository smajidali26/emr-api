using EMR.Application.Common.Interfaces;
using EMR.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EMR.Infrastructure.Services;

/// <summary>
/// HIPAA-compliant audit logging service
/// Logs WHO accessed WHAT, WHEN, from WHERE, and WHY
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
    }

    public Task LogAuthenticationAsync(
        string userId,
        string action,
        string? ipAddress,
        string? userAgent,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var auditData = new
        {
            EventType = "Authentication",
            UserId = userId,
            Action = action,
            IpAddress = ipAddress ?? "Unknown",
            UserAgent = userAgent ?? "Unknown",
            Success = success,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };

        if (success)
        {
            _logger.LogInformation(
                "AUDIT: Authentication | User: {UserId} | Action: {Action} | IP: {IpAddress} | Success: {Success} | Time: {Timestamp}",
                userId, action, ipAddress ?? "Unknown", success, auditData.Timestamp);
        }
        else
        {
            _logger.LogWarning(
                "AUDIT: Authentication Failed | User: {UserId} | Action: {Action} | IP: {IpAddress} | Error: {ErrorMessage} | Time: {Timestamp}",
                userId, action, ipAddress ?? "Unknown", errorMessage, auditData.Timestamp);
        }

        // In production, this should also write to a dedicated audit database or secure log storage
        // For now, we're using structured logging which can be shipped to SIEM systems
        return Task.CompletedTask;
    }

    public Task LogUserRegistrationAsync(
        string userId,
        string email,
        List<UserRole> roles,
        string? ipAddress,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var auditData = new
        {
            EventType = "UserRegistration",
            UserId = userId,
            Email = MaskEmail(email),
            Roles = string.Join(", ", roles),
            IpAddress = ipAddress ?? "Unknown",
            Success = success,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };

        if (success)
        {
            _logger.LogInformation(
                "AUDIT: User Registration | UserId: {UserId} | Email: {Email} | Roles: {Roles} | IP: {IpAddress} | Time: {Timestamp}",
                userId, auditData.Email, auditData.Roles, ipAddress ?? "Unknown", auditData.Timestamp);
        }
        else
        {
            _logger.LogWarning(
                "AUDIT: User Registration Failed | Email: {Email} | IP: {IpAddress} | Error: {ErrorMessage} | Time: {Timestamp}",
                auditData.Email, ipAddress ?? "Unknown", errorMessage, auditData.Timestamp);
        }

        return Task.CompletedTask;
    }

    public Task LogUserAccessAsync(
        string performedBy,
        string action,
        string? targetUserId,
        string? ipAddress,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var auditData = new
        {
            EventType = "UserAccess",
            PerformedBy = performedBy,
            Action = action,
            TargetUserId = targetUserId ?? "N/A",
            IpAddress = ipAddress ?? "Unknown",
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation(
            "AUDIT: User Access | PerformedBy: {PerformedBy} | Action: {Action} | Target: {TargetUserId} | IP: {IpAddress} | Details: {Details} | Time: {Timestamp}",
            performedBy, action, targetUserId ?? "N/A", ipAddress ?? "Unknown", details, auditData.Timestamp);

        return Task.CompletedTask;
    }

    public Task LogDataAccessAsync(
        string userId,
        string action,
        string resourceType,
        string? resourceId,
        string? ipAddress,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var auditData = new
        {
            EventType = "DataAccess",
            UserId = userId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId ?? "N/A",
            IpAddress = ipAddress ?? "Unknown",
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation(
            "AUDIT: Data Access | User: {UserId} | Action: {Action} | Resource: {ResourceType}/{ResourceId} | IP: {IpAddress} | Details: {Details} | Time: {Timestamp}",
            userId, action, resourceType, resourceId ?? "N/A", ipAddress ?? "Unknown", details, auditData.Timestamp);

        return Task.CompletedTask;
    }

    public Task LogPatientRegistrationAsync(
        string patientId,
        string mrn,
        string performedBy,
        string? ipAddress,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var auditData = new
        {
            EventType = "PatientRegistration",
            PatientId = patientId,
            MRN = mrn,
            PerformedBy = performedBy,
            IpAddress = ipAddress ?? "Unknown",
            Success = success,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };

        if (success)
        {
            _logger.LogInformation(
                "AUDIT: Patient Registration | PatientId: {PatientId} | MRN: {MRN} | PerformedBy: {PerformedBy} | IP: {IpAddress} | Time: {Timestamp}",
                patientId, mrn, performedBy, ipAddress ?? "Unknown", auditData.Timestamp);
        }
        else
        {
            _logger.LogWarning(
                "AUDIT: Patient Registration Failed | PerformedBy: {PerformedBy} | IP: {IpAddress} | Error: {ErrorMessage} | Time: {Timestamp}",
                performedBy, ipAddress ?? "Unknown", errorMessage, auditData.Timestamp);
        }

        return Task.CompletedTask;
    }

    public Task LogPatientAccessAsync(
        string patientId,
        string action,
        string performedBy,
        string? ipAddress,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var auditData = new
        {
            EventType = "PatientAccess",
            PatientId = patientId,
            Action = action,
            PerformedBy = performedBy,
            IpAddress = ipAddress ?? "Unknown",
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation(
            "AUDIT: Patient Access | PatientId: {PatientId} | Action: {Action} | PerformedBy: {PerformedBy} | IP: {IpAddress} | Details: {Details} | Time: {Timestamp}",
            patientId, action, performedBy, ipAddress ?? "Unknown", details, auditData.Timestamp);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Mask email for logging to prevent sensitive data exposure
    /// </summary>
    private string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "***";

        var parts = email.Split('@');
        if (parts.Length != 2)
            return "***";

        var localPart = parts[0];
        var domain = parts[1];

        if (localPart.Length <= 2)
            return $"***@{domain}";

        return $"{localPart[0]}***{localPart[^1]}@{domain}";
    }
}

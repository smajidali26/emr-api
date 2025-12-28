using EMR.Domain.Enums;

namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Interface for HIPAA-compliant audit logging service
/// Tracks WHO accessed WHAT, WHEN, from WHERE, and WHY
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Log authentication event (login, logout, registration)
    /// </summary>
    Task LogAuthenticationAsync(
        string userId,
        string action,
        string? ipAddress,
        string? userAgent,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log user registration event
    /// </summary>
    Task LogUserRegistrationAsync(
        string userId,
        string email,
        List<UserRole> roles,
        string? ipAddress,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log user access event
    /// </summary>
    Task LogUserAccessAsync(
        string performedBy,
        string action,
        string? targetUserId,
        string? ipAddress,
        string? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log data access event for HIPAA compliance
    /// </summary>
    Task LogDataAccessAsync(
        string userId,
        string action,
        string resourceType,
        string? resourceId,
        string? ipAddress,
        string? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log patient registration event
    /// </summary>
    Task LogPatientRegistrationAsync(
        string patientId,
        string mrn,
        string performedBy,
        string? ipAddress,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log patient data access for HIPAA compliance
    /// </summary>
    Task LogPatientAccessAsync(
        string patientId,
        string action,
        string performedBy,
        string? ipAddress,
        string? details = null,
        CancellationToken cancellationToken = default);
}

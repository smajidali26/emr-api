using EMR.Application.Common.Interfaces;
using EMR.Domain.Common;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EMR.Infrastructure.Data.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor for automatic audit trail generation
/// Intercepts entity changes and creates audit logs for AuditableEntity types
/// Implements HIPAA-compliant change tracking with PHI masking
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditInterceptor> _logger;

    public AuditInterceptor(
        ICurrentUserService currentUserService,
        ILogger<AuditInterceptor> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            await AuditChangesAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Audit entity changes before saving to database
    /// </summary>
    private async Task AuditChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId()?.ToString() ?? "System";
        var ipAddress = _currentUserService.GetIpAddress();
        var userAgent = _currentUserService.GetUserAgent();

        var auditableEntries = context.ChangeTracker.Entries<AuditableEntity>()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .Where(e => e.Entity.ShouldAuditChanges())
            .ToList();

        if (!auditableEntries.Any())
            return;

        var auditLogs = new List<AuditLog>();

        foreach (var entry in auditableEntries)
        {
            var auditLog = await CreateAuditLogFromEntryAsync(
                entry,
                userId,
                ipAddress,
                userAgent,
                cancellationToken);

            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        // Add audit logs to context
        if (auditLogs.Any())
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }

    /// <summary>
    /// Create audit log entry from entity change
    /// </summary>
    private async Task<AuditLog?> CreateAuditLogFromEntryAsync(
        EntityEntry<AuditableEntity> entry,
        string userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = entry.Entity;
            var eventType = GetEventTypeFromState(entry.State);
            var action = GetActionDescription(entry.State, entity.AuditResourceType);

            var auditLog = new AuditLog(
                eventType: eventType,
                userId: userId,
                action: action,
                resourceType: entity.AuditResourceType,
                resourceId: entity.AuditResourceId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: true,
                details: entity.GetAuditDescription());

            // For modifications, capture old and new values
            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                var oldValues = GetOldValues(entry, entity.GetAuditExcludedProperties());
                var newValues = entry.State == EntityState.Modified
                    ? GetNewValues(entry, entity.GetAuditExcludedProperties())
                    : null;

                auditLog.SetChangeValues(oldValues, newValues);
            }

            return auditLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for entity {EntityType}",
                entry.Entity.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// Map entity state to audit event type
    /// </summary>
    private static AuditEventType GetEventTypeFromState(EntityState state)
    {
        return state switch
        {
            EntityState.Added => AuditEventType.Create,
            EntityState.Modified => AuditEventType.Update,
            EntityState.Deleted => AuditEventType.Delete,
            _ => AuditEventType.View
        };
    }

    /// <summary>
    /// Generate action description from entity state
    /// </summary>
    private static string GetActionDescription(EntityState state, string resourceType)
    {
        return state switch
        {
            EntityState.Added => $"Created {resourceType}",
            EntityState.Modified => $"Updated {resourceType}",
            EntityState.Deleted => $"Deleted {resourceType}",
            _ => $"Modified {resourceType}"
        };
    }

    /// <summary>
    /// Get old values from entity entry (before changes)
    /// Sanitizes PHI fields to prevent sensitive data in audit logs
    /// </summary>
    private string? GetOldValues(EntityEntry entry, IEnumerable<string> excludedProperties)
    {
        try
        {
            var oldValues = new Dictionary<string, object?>();
            var excludedSet = new HashSet<string>(excludedProperties);

            foreach (var property in entry.Properties)
            {
                // Skip excluded properties
                if (excludedSet.Contains(property.Metadata.Name))
                    continue;

                // Only include modified properties
                if (property.IsModified)
                {
                    var value = property.OriginalValue;
                    oldValues[property.Metadata.Name] = SanitizeValue(property.Metadata.Name, value);
                }
            }

            return oldValues.Any()
                ? JsonSerializer.Serialize(oldValues, new JsonSerializerOptions { WriteIndented = false })
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize old values for entity");
            return null;
        }
    }

    /// <summary>
    /// Get new values from entity entry (after changes)
    /// Sanitizes PHI fields to prevent sensitive data in audit logs
    /// </summary>
    private string? GetNewValues(EntityEntry entry, IEnumerable<string> excludedProperties)
    {
        try
        {
            var newValues = new Dictionary<string, object?>();
            var excludedSet = new HashSet<string>(excludedProperties);

            foreach (var property in entry.Properties)
            {
                // Skip excluded properties
                if (excludedSet.Contains(property.Metadata.Name))
                    continue;

                // Only include modified properties
                if (property.IsModified)
                {
                    var value = property.CurrentValue;
                    newValues[property.Metadata.Name] = SanitizeValue(property.Metadata.Name, value);
                }
            }

            return newValues.Any()
                ? JsonSerializer.Serialize(newValues, new JsonSerializerOptions { WriteIndented = false })
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize new values for entity");
            return null;
        }
    }

    /// <summary>
    /// Sanitize values to mask PHI
    /// CRITICAL: This prevents patient names, SSN, and other PHI from being logged
    /// </summary>
    private static object? SanitizeValue(string propertyName, object? value)
    {
        if (value == null)
            return null;

        // List of PHI field patterns to mask
        var phiPatterns = new[]
        {
            "firstname", "lastname", "name", "fullname",
            "ssn", "socialsecurity",
            "dateofbirth", "dob", "birthdate",
            "phone", "phonenumber", "mobile",
            "email",
            "address", "street", "city", "zipcode", "postalcode",
            "diagnosis", "condition", "symptoms",
            "prescription", "medication",
            "notes", "comments"
        };

        var propertyLower = propertyName.ToLower();

        // Check if property name matches any PHI pattern
        if (phiPatterns.Any(pattern => propertyLower.Contains(pattern)))
        {
            // Mask the value based on type
            if (value is string strValue && !string.IsNullOrEmpty(strValue))
            {
                // For strings, show only first and last character
                if (strValue.Length <= 2)
                    return "***";
                return $"{strValue[0]}***{strValue[^1]}";
            }

            if (value is DateTime)
            {
                // For dates, show only year
                return ((DateTime)value).Year.ToString();
            }

            // For other types, return masked indicator
            return "[MASKED]";
        }

        // Non-PHI fields - return actual value
        return value;
    }
}

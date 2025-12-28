using EMR.Domain.Enums;

namespace EMR.Application.Common.Attributes;

/// <summary>
/// Attribute to mark commands/queries for automatic audit logging
/// Apply this to MediatR requests that require audit trail
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class AuditableAttribute : Attribute
{
    /// <summary>
    /// Type of audit event
    /// </summary>
    public AuditEventType EventType { get; }

    /// <summary>
    /// Resource type being accessed (e.g., "Patient", "Encounter")
    /// </summary>
    public string ResourceType { get; }

    /// <summary>
    /// Action description (e.g., "Viewed patient record", "Updated diagnosis")
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Whether this operation accesses PHI
    /// </summary>
    public bool AccessesPhi { get; set; } = false;

    /// <summary>
    /// Initialize the auditable attribute
    /// </summary>
    public AuditableAttribute(AuditEventType eventType, string resourceType, string action)
    {
        EventType = eventType;
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }
}

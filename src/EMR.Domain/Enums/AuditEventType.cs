namespace EMR.Domain.Enums;

/// <summary>
/// Types of audit events for HIPAA compliance tracking
/// Defines all auditable actions in the EMR system
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// View/Read operation - accessing PHI or sensitive data
    /// </summary>
    View = 1,

    /// <summary>
    /// Create operation - adding new records
    /// </summary>
    Create = 2,

    /// <summary>
    /// Update/Modify operation - changing existing records
    /// </summary>
    Update = 3,

    /// <summary>
    /// Delete operation - removing or soft-deleting records
    /// </summary>
    Delete = 4,

    /// <summary>
    /// Export operation - exporting data to external formats (CSV, PDF, etc.)
    /// </summary>
    Export = 5,

    /// <summary>
    /// Print operation - printing PHI or sensitive documents
    /// </summary>
    Print = 6,

    /// <summary>
    /// Successful login event
    /// </summary>
    Login = 7,

    /// <summary>
    /// Logout event
    /// </summary>
    Logout = 8,

    /// <summary>
    /// Failed login attempt
    /// </summary>
    FailedLogin = 9,

    /// <summary>
    /// Search operation - querying for patient records
    /// </summary>
    Search = 10,

    /// <summary>
    /// Share operation - sharing records with other users/systems
    /// </summary>
    Share = 11,

    /// <summary>
    /// Download operation - downloading PHI files
    /// </summary>
    Download = 12,

    /// <summary>
    /// Access denied - unauthorized access attempt
    /// </summary>
    AccessDenied = 13,

    /// <summary>
    /// Configuration change - system settings modification
    /// </summary>
    ConfigurationChange = 14,

    /// <summary>
    /// Emergency access - break-glass scenario
    /// </summary>
    EmergencyAccess = 15
}

namespace EMR.Domain.Enums;

/// <summary>
/// Types of resources that can be protected by authorization
/// Used for attribute-based access control (ABAC)
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// User management resources
    /// </summary>
    User = 1,

    /// <summary>
    /// Patient records
    /// </summary>
    Patient = 2,

    /// <summary>
    /// Clinical encounters
    /// </summary>
    Encounter = 3,

    /// <summary>
    /// Medical orders
    /// </summary>
    Order = 4,

    /// <summary>
    /// Vital signs
    /// </summary>
    VitalSign = 5,

    /// <summary>
    /// Clinical assessments
    /// </summary>
    Assessment = 6,

    /// <summary>
    /// Clinical notes
    /// </summary>
    ClinicalNote = 7,

    /// <summary>
    /// Medications
    /// </summary>
    Medication = 8,

    /// <summary>
    /// Laboratory results
    /// </summary>
    LabResult = 9,

    /// <summary>
    /// Imaging studies
    /// </summary>
    ImagingStudy = 10,

    /// <summary>
    /// Appointments and schedules
    /// </summary>
    Appointment = 11,

    /// <summary>
    /// Billing and claims
    /// </summary>
    Billing = 12,

    /// <summary>
    /// System settings
    /// </summary>
    SystemSettings = 13,

    /// <summary>
    /// Roles and permissions
    /// </summary>
    RolePermission = 14
}

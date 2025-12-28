namespace EMR.Domain.Enums;

/// <summary>
/// User roles in the EMR system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// System administrator with full access
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Medical doctor with patient care privileges
    /// </summary>
    Doctor = 2,

    /// <summary>
    /// Nursing staff with patient care support privileges
    /// </summary>
    Nurse = 3,

    /// <summary>
    /// General administrative and support staff
    /// </summary>
    Staff = 4,

    /// <summary>
    /// Patient with limited access to own records
    /// </summary>
    Patient = 5
}

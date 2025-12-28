namespace EMR.Domain.Enums;

/// <summary>
/// Marital status options for patient demographics
/// </summary>
public enum MaritalStatus
{
    /// <summary>
    /// Single, never married
    /// </summary>
    Single = 1,

    /// <summary>
    /// Legally married
    /// </summary>
    Married = 2,

    /// <summary>
    /// Divorced
    /// </summary>
    Divorced = 3,

    /// <summary>
    /// Widowed
    /// </summary>
    Widowed = 4,

    /// <summary>
    /// Separated but not divorced
    /// </summary>
    Separated = 5,

    /// <summary>
    /// Domestic partnership
    /// </summary>
    DomesticPartner = 6,

    /// <summary>
    /// Unknown or not disclosed
    /// </summary>
    Unknown = 7
}

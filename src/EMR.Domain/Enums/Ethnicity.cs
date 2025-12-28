namespace EMR.Domain.Enums;

/// <summary>
/// Ethnicity categories for patient demographics (US Census Bureau categories)
/// </summary>
public enum Ethnicity
{
    /// <summary>
    /// Not Hispanic or Latino
    /// </summary>
    NotHispanicOrLatino = 1,

    /// <summary>
    /// Hispanic or Latino
    /// </summary>
    HispanicOrLatino = 2,

    /// <summary>
    /// Declined to specify
    /// </summary>
    DeclinedToSpecify = 3,

    /// <summary>
    /// Unknown
    /// </summary>
    Unknown = 4
}

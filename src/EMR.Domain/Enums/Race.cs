namespace EMR.Domain.Enums;

/// <summary>
/// Race categories for patient demographics (US Census Bureau categories)
/// </summary>
public enum Race
{
    /// <summary>
    /// American Indian or Alaska Native
    /// </summary>
    AmericanIndianOrAlaskaNative = 1,

    /// <summary>
    /// Asian
    /// </summary>
    Asian = 2,

    /// <summary>
    /// Black or African American
    /// </summary>
    BlackOrAfricanAmerican = 3,

    /// <summary>
    /// Native Hawaiian or Other Pacific Islander
    /// </summary>
    NativeHawaiianOrOtherPacificIslander = 4,

    /// <summary>
    /// White
    /// </summary>
    White = 5,

    /// <summary>
    /// Two or more races
    /// </summary>
    TwoOrMoreRaces = 6,

    /// <summary>
    /// Other race
    /// </summary>
    Other = 7,

    /// <summary>
    /// Declined to specify
    /// </summary>
    DeclinedToSpecify = 8,

    /// <summary>
    /// Unknown
    /// </summary>
    Unknown = 9
}

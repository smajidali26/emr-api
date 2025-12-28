namespace EMR.Domain.Enums;

/// <summary>
/// Gender identity options for patient demographics
/// </summary>
public enum Gender
{
    /// <summary>
    /// Male gender
    /// </summary>
    Male = 1,

    /// <summary>
    /// Female gender
    /// </summary>
    Female = 2,

    /// <summary>
    /// Non-binary gender identity
    /// </summary>
    NonBinary = 3,

    /// <summary>
    /// Other gender identity
    /// </summary>
    Other = 4,

    /// <summary>
    /// Prefer not to disclose
    /// </summary>
    PreferNotToSay = 5,

    /// <summary>
    /// Unknown or not specified
    /// </summary>
    Unknown = 6
}

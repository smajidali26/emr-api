using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Patients.DTOs;

namespace EMR.Application.Features.Patients.Commands.UpdatePatientDemographics;

/// <summary>
/// Command to update patient demographics
/// </summary>
public record UpdatePatientDemographicsCommand : ICommand<ResultDto<PatientDto>>
{
    /// <summary>
    /// Patient ID to update
    /// </summary>
    public Guid PatientId { get; init; }

    /// <summary>
    /// Updated demographics data
    /// </summary>
    public PatientDemographicsDto Demographics { get; init; } = new();

    /// <summary>
    /// Updated emergency contact (optional)
    /// </summary>
    public EmergencyContactDto? EmergencyContact { get; init; }
}

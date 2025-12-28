using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Patients.DTOs;

namespace EMR.Application.Features.Patients.Queries.GetPatientById;

/// <summary>
/// Query to get patient by ID
/// </summary>
public record GetPatientByIdQuery(Guid PatientId) : IQuery<ResultDto<PatientDto>>;

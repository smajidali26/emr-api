using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Patients.DTOs;

namespace EMR.Application.Features.Patients.Queries.GetPatientByMRN;

/// <summary>
/// Query to get patient by Medical Record Number (MRN)
/// </summary>
public record GetPatientByMRNQuery(string MRN) : IQuery<ResultDto<PatientDto>>;

using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Patients.DTOs;

namespace EMR.Application.Features.Patients.Queries.SearchPatients;

/// <summary>
/// Query to search patients by criteria
/// </summary>
public record SearchPatientsQuery : IQuery<ResultDto<PagedResultDto<PatientSearchResultDto>>>
{
    /// <summary>
    /// Search term (searches in name, MRN, email)
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; init; } = 20;
}

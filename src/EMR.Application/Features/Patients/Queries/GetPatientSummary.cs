using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Domain.ReadModels;

namespace EMR.Application.Features.Patients.Queries;

/// <summary>
/// Query to get patient summary by ID
/// </summary>
public sealed record GetPatientSummaryQuery(Guid PatientId) : IQuery<ResultDto<PatientSummaryDto>>;

/// <summary>
/// DTO for patient summary
/// </summary>
public sealed record PatientSummaryDto
{
    public Guid Id { get; init; }
    public string MRN { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public int Age { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? PrimaryCareProvider { get; init; }
    public DateTime? LastVisitDate { get; init; }
    public int ActiveAlertsCount { get; init; }
    public bool HasActiveAllergies { get; init; }
    public bool HasActiveMedications { get; init; }
}

/// <summary>
/// Handler for getting patient summary
/// Queries the read model for optimal performance
/// </summary>
public sealed class GetPatientSummaryQueryHandler : IQueryHandler<GetPatientSummaryQuery, ResultDto<PatientSummaryDto>>
{
    private readonly IPatientReadModelRepository _repository;

    public GetPatientSummaryQueryHandler(IPatientReadModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<ResultDto<PatientSummaryDto>> Handle(
        GetPatientSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var readModel = await _repository.GetByIdAsync(request.PatientId, cancellationToken);

        if (readModel == null)
        {
            return ResultDto<PatientSummaryDto>.Failure($"Patient with ID {request.PatientId} not found");
        }

        var dto = MapToDto(readModel);
        return ResultDto<PatientSummaryDto>.Success(dto);
    }

    private static PatientSummaryDto MapToDto(PatientSummaryReadModel readModel)
    {
        return new PatientSummaryDto
        {
            Id = readModel.Id,
            MRN = readModel.MRN,
            FirstName = readModel.FirstName,
            LastName = readModel.LastName,
            FullName = readModel.FullName,
            DateOfBirth = readModel.DateOfBirth,
            Age = readModel.Age,
            Gender = readModel.Gender,
            PhoneNumber = readModel.PhoneNumber,
            Email = readModel.Email,
            Status = readModel.Status,
            PrimaryCareProvider = readModel.PrimaryCareProvider,
            LastVisitDate = readModel.LastVisitDate,
            ActiveAlertsCount = readModel.ActiveAlertsCount,
            HasActiveAllergies = readModel.HasActiveAllergies,
            HasActiveMedications = readModel.HasActiveMedications
        };
    }
}

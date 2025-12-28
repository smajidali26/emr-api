using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using FluentValidation;

namespace EMR.Application.Features.Patients.Queries;

/// <summary>
/// Query to search patients by text
/// </summary>
public sealed record SearchPatientsQuery(
    string SearchText,
    int MaxResults = 50) : IQuery<ResultDto<IReadOnlyList<PatientSummaryDto>>>;

/// <summary>
/// Validator for search patients query
/// </summary>
public sealed class SearchPatientsQueryValidator : AbstractValidator<SearchPatientsQuery>
{
    public SearchPatientsQueryValidator()
    {
        RuleFor(x => x.SearchText)
            .NotEmpty()
            .WithMessage("Search text is required")
            .MinimumLength(2)
            .WithMessage("Search text must be at least 2 characters");

        RuleFor(x => x.MaxResults)
            .GreaterThan(0)
            .WithMessage("Max results must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Max results cannot exceed 100");
    }
}

/// <summary>
/// Handler for searching patients
/// Uses optimized read model with full-text search
/// </summary>
public sealed class SearchPatientsQueryHandler : IQueryHandler<SearchPatientsQuery, ResultDto<IReadOnlyList<PatientSummaryDto>>>
{
    private readonly IPatientReadModelRepository _repository;

    public SearchPatientsQueryHandler(IPatientReadModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<ResultDto<IReadOnlyList<PatientSummaryDto>>> Handle(
        SearchPatientsQuery request,
        CancellationToken cancellationToken)
    {
        var readModels = await _repository.SearchAsync(
            request.SearchText,
            request.MaxResults,
            cancellationToken);

        var dtos = readModels.Select(MapToDto).ToList();
        return ResultDto<IReadOnlyList<PatientSummaryDto>>.Success(dtos);
    }

    private static PatientSummaryDto MapToDto(Domain.ReadModels.PatientSummaryReadModel readModel)
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

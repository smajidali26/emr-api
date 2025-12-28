using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Domain.ReadModels;

namespace EMR.Application.Features.Patients.Queries;

/// <summary>
/// Query to get detailed patient information
/// </summary>
public sealed record GetPatientDetailQuery(Guid PatientId) : IQuery<ResultDto<PatientDetailDto>>;

/// <summary>
/// Comprehensive patient detail DTO
/// </summary>
public sealed record PatientDetailDto
{
    public Guid Id { get; init; }
    public string MRN { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? PreferredName { get; init; }
    public DateTime DateOfBirth { get; init; }
    public int Age { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string? BiologicalSex { get; init; }
    public string? PreferredPronouns { get; init; }
    public string? PrimaryPhone { get; init; }
    public string? SecondaryPhone { get; init; }
    public string? Email { get; init; }
    public string? PreferredContactMethod { get; init; }
    public string Status { get; init; } = string.Empty;
    public ProviderInfo? PrimaryCareProvider { get; init; }
    public AddressInfo? Address { get; init; }
    public EmergencyContactInfo? EmergencyContact { get; init; }
    public InsuranceInfo? PrimaryInsurance { get; init; }
    public InsuranceInfo? SecondaryInsurance { get; init; }
    public List<AllergyInfo> ActiveAllergies { get; init; } = new();
    public List<MedicationInfo> ActiveMedications { get; init; } = new();
    public List<ProblemInfo> ActiveProblems { get; init; } = new();
    public VitalSignsInfo? RecentVitals { get; init; }
    public VisitInfo? LastVisit { get; init; }
    public AppointmentInfo? NextAppointment { get; init; }
    public List<AlertInfo> Alerts { get; init; } = new();
    public string? PreferredLanguage { get; init; }
    public string? Race { get; init; }
    public string? Ethnicity { get; init; }
    public string? MaritalStatus { get; init; }
    public string? BloodType { get; init; }
    public bool? IsOrganDonor { get; init; }
    public bool HasAdvanceDirective { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Handler for getting detailed patient information
/// Queries the denormalized read model for optimal performance
/// </summary>
public sealed class GetPatientDetailQueryHandler : IQueryHandler<GetPatientDetailQuery, ResultDto<PatientDetailDto>>
{
    private readonly IPatientDetailReadModelRepository _repository;

    public GetPatientDetailQueryHandler(IPatientDetailReadModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<ResultDto<PatientDetailDto>> Handle(
        GetPatientDetailQuery request,
        CancellationToken cancellationToken)
    {
        var readModel = await _repository.GetByIdAsync(request.PatientId, cancellationToken);

        if (readModel == null)
        {
            return ResultDto<PatientDetailDto>.Failure($"Patient with ID {request.PatientId} not found");
        }

        var dto = MapToDto(readModel);
        return ResultDto<PatientDetailDto>.Success(dto);
    }

    private static PatientDetailDto MapToDto(PatientDetailReadModel readModel)
    {
        return new PatientDetailDto
        {
            Id = readModel.Id,
            MRN = readModel.MRN,
            FirstName = readModel.FirstName,
            MiddleName = readModel.MiddleName,
            LastName = readModel.LastName,
            FullName = readModel.FullName,
            PreferredName = readModel.PreferredName,
            DateOfBirth = readModel.DateOfBirth,
            Age = readModel.Age,
            Gender = readModel.Gender,
            BiologicalSex = readModel.BiologicalSex,
            PreferredPronouns = readModel.PreferredPronouns,
            PrimaryPhone = readModel.PrimaryPhone,
            SecondaryPhone = readModel.SecondaryPhone,
            Email = readModel.Email,
            PreferredContactMethod = readModel.PreferredContactMethod,
            Status = readModel.Status,
            PrimaryCareProvider = readModel.PrimaryCareProvider,
            Address = readModel.Address,
            EmergencyContact = readModel.EmergencyContact,
            PrimaryInsurance = readModel.PrimaryInsurance,
            SecondaryInsurance = readModel.SecondaryInsurance,
            ActiveAllergies = readModel.ActiveAllergies,
            ActiveMedications = readModel.ActiveMedications,
            ActiveProblems = readModel.ActiveProblems,
            RecentVitals = readModel.RecentVitals,
            LastVisit = readModel.LastVisit,
            NextAppointment = readModel.NextAppointment,
            Alerts = readModel.Alerts,
            PreferredLanguage = readModel.PreferredLanguage,
            Race = readModel.Race,
            Ethnicity = readModel.Ethnicity,
            MaritalStatus = readModel.MaritalStatus,
            BloodType = readModel.BloodType,
            IsOrganDonor = readModel.IsOrganDonor,
            HasAdvanceDirective = readModel.HasAdvanceDirective,
            CreatedAt = readModel.CreatedAt
        };
    }
}

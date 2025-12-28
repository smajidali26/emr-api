using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Features.Patients.DTOs;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Patients.Queries.GetPatientByMRN;

/// <summary>
/// Handler for GetPatientByMRNQuery
/// HIPAA Compliance: Logs all patient data access
/// </summary>
public class GetPatientByMRNQueryHandler : IQueryHandler<GetPatientByMRNQuery, ResultDto<PatientDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<GetPatientByMRNQueryHandler> _logger;

    public GetPatientByMRNQueryHandler(
        IPatientRepository patientRepository,
        ICurrentUserService currentUserService,
        IAuditLogger auditLogger,
        ILogger<GetPatientByMRNQueryHandler> logger)
    {
        _patientRepository = patientRepository;
        _currentUserService = currentUserService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<ResultDto<PatientDto>> Handle(GetPatientByMRNQuery request, CancellationToken cancellationToken)
    {
        var ipAddress = _currentUserService.GetIpAddress();
        var performedBy = _currentUserService.GetUserEmail() ?? "system";

        try
        {
            _logger.LogInformation("Getting patient by MRN: {MRN}", request.MRN);

            var patient = await _patientRepository.GetByMrnAsync(request.MRN, cancellationToken);

            if (patient == null)
            {
                _logger.LogWarning("Patient not found with MRN: {MRN}", request.MRN);
                return ResultDto<PatientDto>.Failure("Patient not found");
            }

            // Audit log patient access
            await _auditLogger.LogPatientAccessAsync(
                patientId: patient.Id.ToString(),
                action: "ViewPatientByMRN",
                performedBy: performedBy,
                ipAddress: ipAddress,
                details: $"Accessed patient record via MRN: {request.MRN}",
                cancellationToken: cancellationToken);

            var patientDto = MapToDto(patient);

            return ResultDto<PatientDto>.Success(patientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient by MRN: {MRN}", request.MRN);
            return ResultDto<PatientDto>.Failure("An error occurred while retrieving patient information");
        }
    }

    private static PatientDto MapToDto(Domain.Entities.Patient patient)
    {
        return new PatientDto
        {
            Id = patient.Id,
            MedicalRecordNumber = patient.MedicalRecordNumber.Value,
            FirstName = patient.FirstName,
            MiddleName = patient.MiddleName,
            LastName = patient.LastName,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            Age = patient.Age,
            Gender = patient.Gender,
            PhoneNumber = patient.PhoneNumber,
            AlternatePhoneNumber = patient.AlternatePhoneNumber,
            Email = patient.Email,
            Address = new PatientAddressDto
            {
                Street = patient.Address.Street,
                Street2 = patient.Address.Street2,
                City = patient.Address.City,
                State = patient.Address.State,
                ZipCode = patient.Address.ZipCode,
                Country = patient.Address.Country
            },
            MaritalStatus = patient.MaritalStatus,
            Race = patient.Race,
            Ethnicity = patient.Ethnicity,
            PreferredLanguage = patient.PreferredLanguage,
            EmergencyContact = new EmergencyContactDto
            {
                Name = patient.EmergencyContact.Name,
                Relationship = patient.EmergencyContact.Relationship,
                PhoneNumber = patient.EmergencyContact.PhoneNumber,
                AlternatePhoneNumber = patient.EmergencyContact.AlternatePhoneNumber
            },
            IsActive = patient.IsActive,
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt
        };
    }
}

using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Features.Patients.DTOs;
using EMR.Domain.Enums;
using EMR.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Patients.Queries.GetPatientById;

/// <summary>
/// Handler for GetPatientByIdQuery
/// HIPAA Compliance: Logs all patient data access
/// SECURITY: Authorization check required before data access
/// </summary>
public class GetPatientByIdQueryHandler : IQueryHandler<GetPatientByIdQuery, ResultDto<PatientDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<GetPatientByIdQueryHandler> _logger;

    public GetPatientByIdQueryHandler(
        IPatientRepository patientRepository,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IAuditLogger auditLogger,
        ILogger<GetPatientByIdQueryHandler> logger)
    {
        _patientRepository = patientRepository;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<ResultDto<PatientDto>> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var ipAddress = _currentUserService.GetIpAddress();
        var performedBy = _currentUserService.GetUserEmail() ?? "system";
        var userId = _currentUserService.GetUserId();

        try
        {
            _logger.LogInformation("Getting patient by ID: {PatientId}", request.PatientId);

            // SECURITY FIX: Authorization check BEFORE fetching data
            // This enforces HIPAA's "minimum necessary" standard
            var hasAccess = await _authorizationService.HasResourceAccessAsync(
                ResourceType.Patient,
                request.PatientId,
                Permission.PatientsView,
                cancellationToken);

            if (!hasAccess)
            {
                _logger.LogWarning(
                    "AUTHORIZATION_DENIED | UserId: {UserId} | Resource: Patient/{PatientId} | Permission: PatientsView",
                    userId,
                    request.PatientId);

                // Audit log the denied access attempt
                await _auditLogger.LogDataAccessAsync(
                    userId: userId?.ToString() ?? "unknown",
                    action: "ViewPatient_Denied",
                    resourceType: "Patient",
                    resourceId: request.PatientId.ToString(),
                    ipAddress: ipAddress,
                    details: "Access denied - insufficient authorization",
                    cancellationToken: cancellationToken);

                return ResultDto<PatientDto>.Failure("You do not have access to this patient record");
            }

            var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);

            if (patient == null)
            {
                _logger.LogWarning("Patient not found with ID: {PatientId}", request.PatientId);
                return ResultDto<PatientDto>.Failure("Patient not found");
            }

            // Audit log patient access
            await _auditLogger.LogPatientAccessAsync(
                patientId: patient.Id.ToString(),
                action: "ViewPatient",
                performedBy: performedBy,
                ipAddress: ipAddress,
                details: $"Accessed patient record - MRN: {patient.MedicalRecordNumber.Value}",
                cancellationToken: cancellationToken);

            var patientDto = MapToDto(patient);

            return ResultDto<PatientDto>.Success(patientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient by ID: {PatientId}", request.PatientId);
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

using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Common.Utilities;
using EMR.Application.Features.Patients.DTOs;
using EMR.Domain.Entities;
using EMR.Domain.Interfaces;
using EMR.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Patients.Commands.RegisterPatient;

/// <summary>
/// Handler for RegisterPatientCommand
/// HIPAA Compliance: Logs all patient registration activities for audit trail
/// </summary>
public class RegisterPatientCommandHandler : ICommandHandler<RegisterPatientCommand, ResultDto<PatientDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<RegisterPatientCommandHandler> _logger;

    public RegisterPatientCommandHandler(
        IPatientRepository patientRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuditLogger auditLogger,
        ILogger<RegisterPatientCommandHandler> logger)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<ResultDto<PatientDto>> Handle(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        var ipAddress = _currentUserService.GetIpAddress();
        var createdBy = _currentUserService.GetUserEmail() ?? "system";
        var userId = _currentUserService.GetUserId()?.ToString() ?? "system";

        try
        {
            // HIPAA FIX: Removed DOB from log - PHI should not be logged in plaintext
            _logger.LogInformation("Registering new patient: {FirstName} {LastName}",
                LogSanitizer.SanitizePersonName(request.FirstName),
                LogSanitizer.SanitizePersonName(request.LastName));

            // Check for potential duplicate patients
            var potentialDuplicates = await _patientRepository.FindPotentialDuplicatesAsync(
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                cancellationToken);

            if (potentialDuplicates.Any())
            {
                // HIPAA FIX: Removed DOB from log - PHI should not be logged in plaintext
                _logger.LogWarning("Potential duplicate patient found during registration. FirstName: {FirstName}, LastName: {LastName}, Count: {DuplicateCount}",
                    LogSanitizer.SanitizePersonName(request.FirstName),
                    LogSanitizer.SanitizePersonName(request.LastName),
                    potentialDuplicates.Count());

                // Log but don't block registration - let staff handle duplicates
            }

            // Create address value object
            var address = PatientAddress.Create(
                request.Address.Street,
                request.Address.Street2,
                request.Address.City,
                request.Address.State,
                request.Address.ZipCode,
                request.Address.Country);

            // Create emergency contact value object
            var emergencyContact = EmergencyContact.Create(
                request.EmergencyContact.Name,
                request.EmergencyContact.Relationship,
                request.EmergencyContact.PhoneNumber,
                request.EmergencyContact.AlternatePhoneNumber);

            // Create patient entity
            var patient = new Patient(
                firstName: request.FirstName,
                lastName: request.LastName,
                dateOfBirth: request.DateOfBirth,
                gender: request.Gender,
                phoneNumber: request.PhoneNumber,
                address: address,
                emergencyContact: emergencyContact,
                createdBy: userId,
                middleName: request.MiddleName,
                email: request.Email,
                alternatePhoneNumber: request.AlternatePhoneNumber,
                socialSecurityNumber: request.SocialSecurityNumber,
                maritalStatus: request.MaritalStatus,
                race: request.Race,
                ethnicity: request.Ethnicity,
                preferredLanguage: request.PreferredLanguage);

            // Add patient to repository
            await _patientRepository.AddAsync(patient, cancellationToken);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Patient registered successfully. MRN: {MRN}, PatientId: {PatientId}",
                patient.MedicalRecordNumber.Value,
                patient.Id);

            // Audit log the registration
            await _auditLogger.LogPatientRegistrationAsync(
                patientId: patient.Id.ToString(),
                mrn: patient.MedicalRecordNumber.Value,
                performedBy: createdBy,
                ipAddress: ipAddress,
                success: true,
                cancellationToken: cancellationToken);

            // Map to DTO
            var patientDto = MapToDto(patient);

            return ResultDto<PatientDto>.Success(patientDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Validation error during patient registration: {Message}", ex.Message);

            await _auditLogger.LogPatientRegistrationAsync(
                patientId: "N/A",
                mrn: "N/A",
                performedBy: createdBy,
                ipAddress: ipAddress,
                success: false,
                errorMessage: ex.Message,
                cancellationToken: cancellationToken);

            return ResultDto<PatientDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering patient: {FirstName} {LastName}",
                LogSanitizer.SanitizePersonName(request.FirstName),
                LogSanitizer.SanitizePersonName(request.LastName));

            await _auditLogger.LogPatientRegistrationAsync(
                patientId: "N/A",
                mrn: "N/A",
                performedBy: createdBy,
                ipAddress: ipAddress,
                success: false,
                errorMessage: "An unexpected error occurred",
                cancellationToken: cancellationToken);

            return ResultDto<PatientDto>.Failure("An error occurred while registering the patient. Please try again.");
        }
    }

    private static PatientDto MapToDto(Patient patient)
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

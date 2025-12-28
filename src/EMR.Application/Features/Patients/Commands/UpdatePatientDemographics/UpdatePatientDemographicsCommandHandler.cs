using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Common.Interfaces;
using EMR.Application.Common.Utilities;
using EMR.Application.Features.Patients.DTOs;
using EMR.Domain.Interfaces;
using EMR.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Patients.Commands.UpdatePatientDemographics;

/// <summary>
/// Handler for UpdatePatientDemographicsCommand
/// HIPAA Compliance: Logs all patient demographic update activities
/// </summary>
public class UpdatePatientDemographicsCommandHandler : ICommandHandler<UpdatePatientDemographicsCommand, ResultDto<PatientDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<UpdatePatientDemographicsCommandHandler> _logger;

    public UpdatePatientDemographicsCommandHandler(
        IPatientRepository patientRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuditLogger auditLogger,
        ILogger<UpdatePatientDemographicsCommandHandler> logger)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<ResultDto<PatientDto>> Handle(UpdatePatientDemographicsCommand request, CancellationToken cancellationToken)
    {
        var ipAddress = _currentUserService.GetIpAddress();
        var updatedBy = _currentUserService.GetUserEmail() ?? "system";
        var userId = _currentUserService.GetUserId()?.ToString() ?? "system";

        try
        {
            _logger.LogInformation("Updating patient demographics for PatientId: {PatientId}", request.PatientId);

            // Get patient
            var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
            if (patient == null)
            {
                _logger.LogWarning("Patient not found with ID: {PatientId}", request.PatientId);
                return ResultDto<PatientDto>.Failure("Patient not found");
            }

            // Create address value object
            var address = PatientAddress.Create(
                request.Demographics.Address.Street,
                request.Demographics.Address.Street2,
                request.Demographics.Address.City,
                request.Demographics.Address.State,
                request.Demographics.Address.ZipCode,
                request.Demographics.Address.Country);

            // Update demographics
            patient.UpdateDemographics(
                firstName: request.Demographics.FirstName,
                lastName: request.Demographics.LastName,
                dateOfBirth: request.Demographics.DateOfBirth,
                gender: request.Demographics.Gender,
                phoneNumber: request.Demographics.PhoneNumber,
                address: address,
                updatedBy: userId,
                middleName: request.Demographics.MiddleName,
                email: request.Demographics.Email,
                alternatePhoneNumber: request.Demographics.AlternatePhoneNumber,
                maritalStatus: request.Demographics.MaritalStatus,
                race: request.Demographics.Race,
                ethnicity: request.Demographics.Ethnicity,
                preferredLanguage: request.Demographics.PreferredLanguage);

            // Update emergency contact if provided
            if (request.EmergencyContact != null)
            {
                var emergencyContact = EmergencyContact.Create(
                    request.EmergencyContact.Name,
                    request.EmergencyContact.Relationship,
                    request.EmergencyContact.PhoneNumber,
                    request.EmergencyContact.AlternatePhoneNumber);

                patient.UpdateEmergencyContact(emergencyContact, userId);
            }

            // Update repository
            _patientRepository.Update(patient);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Patient demographics updated successfully. PatientId: {PatientId}, MRN: {MRN}",
                patient.Id, patient.MedicalRecordNumber.Value);

            // Audit log the update
            await _auditLogger.LogPatientAccessAsync(
                patientId: patient.Id.ToString(),
                action: "UpdateDemographics",
                performedBy: updatedBy,
                ipAddress: ipAddress,
                details: "Patient demographics updated",
                cancellationToken: cancellationToken);

            // Map to DTO
            var patientDto = MapToDto(patient);

            return ResultDto<PatientDto>.Success(patientDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Validation error during patient demographics update: {Message}", ex.Message);

            await _auditLogger.LogPatientAccessAsync(
                patientId: request.PatientId.ToString(),
                action: "UpdateDemographics",
                performedBy: updatedBy,
                ipAddress: ipAddress,
                details: $"Failed: {ex.Message}",
                cancellationToken: cancellationToken);

            return ResultDto<PatientDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient demographics for PatientId: {PatientId}", request.PatientId);

            await _auditLogger.LogPatientAccessAsync(
                patientId: request.PatientId.ToString(),
                action: "UpdateDemographics",
                performedBy: updatedBy,
                ipAddress: ipAddress,
                details: "Failed: Unexpected error",
                cancellationToken: cancellationToken);

            return ResultDto<PatientDto>.Failure("An error occurred while updating patient demographics. Please try again.");
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

using FluentValidation;

namespace EMR.Application.Features.Patients.Commands.UpdatePatientDemographics;

/// <summary>
/// Validator for UpdatePatientDemographicsCommand
/// </summary>
public class UpdatePatientDemographicsCommandValidator : AbstractValidator<UpdatePatientDemographicsCommand>
{
    public UpdatePatientDemographicsCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required");

        RuleFor(x => x.Demographics.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("First name contains invalid characters");

        RuleFor(x => x.Demographics.MiddleName)
            .MaximumLength(100).WithMessage("Middle name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Middle name contains invalid characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Demographics.MiddleName));

        RuleFor(x => x.Demographics.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Last name contains invalid characters");

        RuleFor(x => x.Demographics.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .LessThan(DateTime.UtcNow.Date.AddDays(1)).WithMessage("Date of birth cannot be in the future")
            .GreaterThan(DateTime.UtcNow.AddYears(-150)).WithMessage("Date of birth is not valid");

        RuleFor(x => x.Demographics.Gender)
            .IsInEnum().WithMessage("Invalid gender value");

        RuleFor(x => x.Demographics.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^[\d\s\-\(\)\+\.]+$").WithMessage("Phone number contains invalid characters")
            .MinimumLength(10).WithMessage("Phone number must be at least 10 characters")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");

        RuleFor(x => x.Demographics.AlternatePhoneNumber)
            .Matches(@"^[\d\s\-\(\)\+\.]+$").WithMessage("Alternate phone number contains invalid characters")
            .MinimumLength(10).WithMessage("Alternate phone number must be at least 10 characters")
            .MaximumLength(20).WithMessage("Alternate phone number cannot exceed 20 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Demographics.AlternatePhoneNumber));

        RuleFor(x => x.Demographics.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Demographics.Email));

        // Address validation
        RuleFor(x => x.Demographics.Address.Street)
            .NotEmpty().WithMessage("Street address is required")
            .MaximumLength(200).WithMessage("Street address cannot exceed 200 characters");

        RuleFor(x => x.Demographics.Address.Street2)
            .MaximumLength(200).WithMessage("Street address line 2 cannot exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Demographics.Address.Street2));

        RuleFor(x => x.Demographics.Address.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.Demographics.Address.State)
            .NotEmpty().WithMessage("State is required")
            .MaximumLength(50).WithMessage("State cannot exceed 50 characters");

        RuleFor(x => x.Demographics.Address.ZipCode)
            .NotEmpty().WithMessage("Zip code is required")
            .MaximumLength(20).WithMessage("Zip code cannot exceed 20 characters");

        RuleFor(x => x.Demographics.Address.Country)
            .NotEmpty().WithMessage("Country is required")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");

        // Emergency contact validation (when provided)
        When(x => x.EmergencyContact != null, () =>
        {
            RuleFor(x => x.EmergencyContact!.Name)
                .NotEmpty().WithMessage("Emergency contact name is required")
                .MaximumLength(200).WithMessage("Emergency contact name cannot exceed 200 characters");

            RuleFor(x => x.EmergencyContact!.Relationship)
                .NotEmpty().WithMessage("Emergency contact relationship is required")
                .MaximumLength(100).WithMessage("Emergency contact relationship cannot exceed 100 characters");

            RuleFor(x => x.EmergencyContact!.PhoneNumber)
                .NotEmpty().WithMessage("Emergency contact phone number is required")
                .Matches(@"^[\d\s\-\(\)\+\.]+$").WithMessage("Emergency contact phone number contains invalid characters")
                .MinimumLength(10).WithMessage("Emergency contact phone number must be at least 10 characters")
                .MaximumLength(20).WithMessage("Emergency contact phone number cannot exceed 20 characters");

            RuleFor(x => x.EmergencyContact!.AlternatePhoneNumber)
                .Matches(@"^[\d\s\-\(\)\+\.]+$").WithMessage("Emergency contact alternate phone number contains invalid characters")
                .MinimumLength(10).WithMessage("Emergency contact alternate phone number must be at least 10 characters")
                .MaximumLength(20).WithMessage("Emergency contact alternate phone number cannot exceed 20 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.EmergencyContact!.AlternatePhoneNumber));
        });

        RuleFor(x => x.Demographics.MaritalStatus)
            .IsInEnum().WithMessage("Invalid marital status value")
            .When(x => x.Demographics.MaritalStatus.HasValue);

        RuleFor(x => x.Demographics.Race)
            .IsInEnum().WithMessage("Invalid race value")
            .When(x => x.Demographics.Race.HasValue);

        RuleFor(x => x.Demographics.Ethnicity)
            .IsInEnum().WithMessage("Invalid ethnicity value")
            .When(x => x.Demographics.Ethnicity.HasValue);

        RuleFor(x => x.Demographics.PreferredLanguage)
            .IsInEnum().WithMessage("Invalid preferred language value")
            .When(x => x.Demographics.PreferredLanguage.HasValue);
    }
}

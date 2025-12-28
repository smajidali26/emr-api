using FluentValidation;

namespace EMR.Application.Features.Patients.Commands.RegisterPatient;

/// <summary>
/// Validator for RegisterPatientCommand
/// </summary>
public class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("First name contains invalid characters");

        RuleFor(x => x.MiddleName)
            .MaximumLength(100).WithMessage("Middle name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Middle name contains invalid characters")
            .When(x => !string.IsNullOrWhiteSpace(x.MiddleName));

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Last name contains invalid characters");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .LessThan(DateTime.UtcNow.Date.AddDays(1)).WithMessage("Date of birth cannot be in the future")
            .GreaterThan(DateTime.UtcNow.AddYears(-150)).WithMessage("Date of birth is not valid");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value");

        RuleFor(x => x.SocialSecurityNumber)
            .Matches(@"^\d{3}-\d{2}-\d{4}$").WithMessage("SSN must be in format XXX-XX-XXXX")
            .When(x => !string.IsNullOrWhiteSpace(x.SocialSecurityNumber));

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^[\d\s\-\(\)\+\.]+$").WithMessage("Phone number contains invalid characters")
            .MinimumLength(10).WithMessage("Phone number must be at least 10 characters")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");

        RuleFor(x => x.AlternatePhoneNumber)
            .Matches(@"^[\d\s\-\(\)\+\.]+$").WithMessage("Alternate phone number contains invalid characters")
            .MinimumLength(10).WithMessage("Alternate phone number must be at least 10 characters")
            .MaximumLength(20).WithMessage("Alternate phone number cannot exceed 20 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.AlternatePhoneNumber));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        // Address validation
        RuleFor(x => x.Address.Street)
            .NotEmpty().WithMessage("Street address is required")
            .MaximumLength(200).WithMessage("Street address cannot exceed 200 characters");

        RuleFor(x => x.Address.Street2)
            .MaximumLength(200).WithMessage("Street address line 2 cannot exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Address.Street2));

        RuleFor(x => x.Address.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.Address.State)
            .NotEmpty().WithMessage("State is required")
            .MaximumLength(50).WithMessage("State cannot exceed 50 characters");

        RuleFor(x => x.Address.ZipCode)
            .NotEmpty().WithMessage("Zip code is required")
            .MaximumLength(20).WithMessage("Zip code cannot exceed 20 characters");

        RuleFor(x => x.Address.Country)
            .NotEmpty().WithMessage("Country is required")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");

        // Emergency contact validation
        RuleFor(x => x.EmergencyContact.Name)
            .NotEmpty().WithMessage("Emergency contact name is required")
            .MaximumLength(200).WithMessage("Emergency contact name cannot exceed 200 characters");

        RuleFor(x => x.EmergencyContact.Relationship)
            .NotEmpty().WithMessage("Emergency contact relationship is required")
            .MaximumLength(100).WithMessage("Emergency contact relationship cannot exceed 100 characters");

        RuleFor(x => x.EmergencyContact.PhoneNumber)
            .NotEmpty().WithMessage("Emergency contact phone number is required")
            .Matches(@"^[\d\s\-\(\)\+\.]+$").WithMessage("Emergency contact phone number contains invalid characters")
            .MinimumLength(10).WithMessage("Emergency contact phone number must be at least 10 characters")
            .MaximumLength(20).WithMessage("Emergency contact phone number cannot exceed 20 characters");

        RuleFor(x => x.EmergencyContact.AlternatePhoneNumber)
            .Matches(@"^[\d\s\-\(\)\+\.]+$").WithMessage("Emergency contact alternate phone number contains invalid characters")
            .MinimumLength(10).WithMessage("Emergency contact alternate phone number must be at least 10 characters")
            .MaximumLength(20).WithMessage("Emergency contact alternate phone number cannot exceed 20 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.EmergencyContact.AlternatePhoneNumber));

        RuleFor(x => x.MaritalStatus)
            .IsInEnum().WithMessage("Invalid marital status value");

        RuleFor(x => x.Race)
            .IsInEnum().WithMessage("Invalid race value");

        RuleFor(x => x.Ethnicity)
            .IsInEnum().WithMessage("Invalid ethnicity value");

        RuleFor(x => x.PreferredLanguage)
            .IsInEnum().WithMessage("Invalid preferred language value");
    }
}

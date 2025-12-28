using EMR.Domain.Enums;
using FluentValidation;

namespace EMR.Application.Features.Auth.Commands.RegisterUser;

/// <summary>
/// Validator for RegisterUserCommand
/// </summary>
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.AzureAdB2CId)
            .NotEmpty().WithMessage("Azure AD B2C ID is required.")
            .MaximumLength(255).WithMessage("Azure AD B2C ID must not exceed 255 characters.")
            .Must(BeValidGuid).WithMessage("Azure AD B2C ID must be a valid GUID format.");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role is required.")
            .Must(roles => roles != null && roles.Count > 0)
            .WithMessage("User must have at least one role.")
            .Must(HaveNoDuplicates).WithMessage("Duplicate roles are not allowed.")
            .Must(HaveValidEnumValues).WithMessage("One or more roles are invalid.")
            .Must(NotHaveConflictingRoles).WithMessage("Patient role cannot be combined with Doctor, Nurse, Staff, or Admin roles.");
    }

    /// <summary>
    /// Validate that Azure AD B2C ID is a valid GUID
    /// </summary>
    private bool BeValidGuid(string azureAdB2CId)
    {
        return Guid.TryParse(azureAdB2CId, out _);
    }

    /// <summary>
    /// Validate that there are no duplicate roles
    /// </summary>
    private bool HaveNoDuplicates(List<UserRole> roles)
    {
        if (roles == null) return true;
        return roles.Count == roles.Distinct().Count();
    }

    /// <summary>
    /// Validate that all roles are valid enum values
    /// </summary>
    private bool HaveValidEnumValues(List<UserRole> roles)
    {
        if (roles == null) return true;
        return roles.All(role => Enum.IsDefined(typeof(UserRole), role));
    }

    /// <summary>
    /// Business rule: Patient role cannot be combined with medical/administrative roles
    /// </summary>
    private bool NotHaveConflictingRoles(List<UserRole> roles)
    {
        if (roles == null || !roles.Any()) return true;

        var hasPatientRole = roles.Contains(UserRole.Patient);
        var hasMedicalOrAdminRole = roles.Any(r =>
            r == UserRole.Doctor ||
            r == UserRole.Nurse ||
            r == UserRole.Staff ||
            r == UserRole.Admin);

        // If user has Patient role, they cannot have any medical/admin roles
        if (hasPatientRole && hasMedicalOrAdminRole)
        {
            return false;
        }

        return true;
    }
}

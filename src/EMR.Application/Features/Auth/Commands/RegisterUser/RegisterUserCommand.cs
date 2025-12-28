using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Auth.DTOs;
using EMR.Domain.Enums;

namespace EMR.Application.Features.Auth.Commands.RegisterUser;

/// <summary>
/// Command to register a new user after Azure AD B2C signup
/// </summary>
public record RegisterUserCommand : ICommand<ResultDto<UserDto>>
{
    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Azure AD B2C unique identifier
    /// </summary>
    public string AzureAdB2CId { get; init; } = string.Empty;

    /// <summary>
    /// User's roles in the system
    /// </summary>
    public List<UserRole> Roles { get; init; } = new();
}

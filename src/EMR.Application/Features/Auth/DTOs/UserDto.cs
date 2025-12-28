using EMR.Domain.Enums;

namespace EMR.Application.Features.Auth.DTOs;

/// <summary>
/// Data transfer object for user information
/// </summary>
public class UserDto
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public Guid Id { get; init; }

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
    /// User's full name
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Azure AD B2C unique identifier
    /// </summary>
    public string AzureAdB2CId { get; init; } = string.Empty;

    /// <summary>
    /// User's roles in the system
    /// </summary>
    public List<UserRole> Roles { get; init; } = new();

    /// <summary>
    /// Indicates whether the user account is active
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Timestamp of the user's last login (UTC)
    /// </summary>
    public DateTime? LastLoginAt { get; init; }

    /// <summary>
    /// Date and time when the user was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Date and time when the user was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}

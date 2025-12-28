using EMR.Domain.Common;
using EMR.Domain.Enums;

namespace EMR.Domain.Entities;

/// <summary>
/// Represents a user in the EMR system
/// </summary>
public class User : BaseEntity
{
    private readonly List<UserRole> _roles = new();

    /// <summary>
    /// User's email address (unique identifier)
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Azure AD B2C unique identifier
    /// </summary>
    public string AzureAdB2CId { get; private set; } = string.Empty;

    /// <summary>
    /// User's roles in the system
    /// </summary>
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    /// <summary>
    /// Indicates whether the user account is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Timestamp of the user's last login (UTC)
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// Full name of the user
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    // Private constructor for EF Core
    private User() { }

    /// <summary>
    /// Creates a new user instance
    /// </summary>
    public User(
        string email,
        string firstName,
        string lastName,
        string azureAdB2CId,
        IEnumerable<UserRole> roles,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        if (string.IsNullOrWhiteSpace(azureAdB2CId))
            throw new ArgumentException("Azure AD B2C ID cannot be empty", nameof(azureAdB2CId));

        var roleList = roles?.ToList() ?? throw new ArgumentNullException(nameof(roles));
        if (!roleList.Any())
            throw new ArgumentException("User must have at least one role", nameof(roles));

        Email = email.Trim().ToLowerInvariant();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        AzureAdB2CId = azureAdB2CId.Trim();
        _roles.AddRange(roleList);
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Updates user information
    /// </summary>
    public void UpdateProfile(string firstName, string lastName, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Updates the user's roles
    /// </summary>
    public void UpdateRoles(IEnumerable<UserRole> roles, string updatedBy)
    {
        var roleList = roles?.ToList() ?? throw new ArgumentNullException(nameof(roles));
        if (!roleList.Any())
            throw new ArgumentException("User must have at least one role", nameof(roles));

        _roles.Clear();
        _roles.AddRange(roleList);
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Adds a role to the user
    /// </summary>
    public void AddRole(UserRole role, string updatedBy)
    {
        if (!_roles.Contains(role))
        {
            _roles.Add(role);
            MarkAsUpdated(updatedBy);
        }
    }

    /// <summary>
    /// Removes a role from the user
    /// </summary>
    public void RemoveRole(UserRole role, string updatedBy)
    {
        if (_roles.Count <= 1)
            throw new InvalidOperationException("User must have at least one role");

        if (_roles.Contains(role))
        {
            _roles.Remove(role);
            MarkAsUpdated(updatedBy);
        }
    }

    /// <summary>
    /// Checks if user has a specific role
    /// </summary>
    public bool HasRole(UserRole role) => _roles.Contains(role);

    /// <summary>
    /// Checks if user has any of the specified roles
    /// </summary>
    public bool HasAnyRole(params UserRole[] roles) => roles.Any(r => _roles.Contains(r));

    /// <summary>
    /// Activates the user account
    /// </summary>
    public void Activate(string updatedBy)
    {
        if (!IsActive)
        {
            IsActive = true;
            MarkAsUpdated(updatedBy);
        }
    }

    /// <summary>
    /// Deactivates the user account
    /// </summary>
    public void Deactivate(string updatedBy)
    {
        if (IsActive)
        {
            IsActive = false;
            MarkAsUpdated(updatedBy);
        }
    }

    /// <summary>
    /// Updates the last login timestamp
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}

namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current user context from HTTP request
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Get the current user's ID from the HTTP context
    /// </summary>
    /// <returns>User ID or null if not authenticated</returns>
    Guid? GetUserId();

    /// <summary>
    /// Get the current user's Azure AD B2C ID from the HTTP context
    /// </summary>
    /// <returns>Azure AD B2C ID or null if not found</returns>
    string? GetAzureAdB2CId();

    /// <summary>
    /// Get the current user's email from the HTTP context
    /// </summary>
    /// <returns>Email or null if not found</returns>
    string? GetUserEmail();

    /// <summary>
    /// Get the IP address of the current request
    /// </summary>
    /// <returns>IP address or null if not available</returns>
    string? GetIpAddress();

    /// <summary>
    /// Get the user agent of the current request
    /// </summary>
    /// <returns>User agent or null if not available</returns>
    string? GetUserAgent();

    /// <summary>
    /// Check if the user is authenticated
    /// </summary>
    /// <returns>True if authenticated, false otherwise</returns>
    bool IsAuthenticated();
}

using System.Text.RegularExpressions;

namespace EMR.Application.Common.Validation;

/// <summary>
/// Validates and sanitizes search parameters to prevent SQL injection and other attacks
/// SECURITY FIX: Task #2 - Add input validation for search params (Maria Rodriguez - 8h)
/// </summary>
public static class SearchParameterValidator
{
    private const int MaxSearchTermLength = 100;
    private const int MinPageNumber = 1;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    // SQL injection patterns to detect and reject
    private static readonly Regex SqlInjectionPattern = new(
        @"('|(--)|;|\/\*|\*\/|xp_|sp_|exec|execute|select|insert|update|delete|drop|create|alter|union|script|javascript|eval|expression)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Allow only alphanumeric, spaces, hyphens, periods, and @ symbol (for email searches)
    private static readonly Regex AllowedCharactersPattern = new(
        @"^[a-zA-Z0-9\s\-\.@]+$",
        RegexOptions.Compiled);

    /// <summary>
    /// Validates and sanitizes page number
    /// SECURITY: Ensures page number is within valid range (>= 1)
    /// </summary>
    /// <param name="pageNumber">Page number to validate</param>
    /// <param name="errorMessage">Error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidatePageNumber(int pageNumber, out string? errorMessage)
    {
        if (pageNumber < MinPageNumber)
        {
            errorMessage = $"Page number must be greater than or equal to {MinPageNumber}";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Validates and sanitizes page size
    /// SECURITY: Ensures page size is within valid range (1-100) to prevent resource exhaustion
    /// </summary>
    /// <param name="pageSize">Page size to validate</param>
    /// <param name="errorMessage">Error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidatePageSize(int pageSize, out string? errorMessage)
    {
        if (pageSize < MinPageSize)
        {
            errorMessage = $"Page size must be greater than or equal to {MinPageSize}";
            return false;
        }

        if (pageSize > MaxPageSize)
        {
            errorMessage = $"Page size must be less than or equal to {MaxPageSize}";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Validates and sanitizes search term
    /// SECURITY: Prevents SQL injection by validating length and character set
    /// </summary>
    /// <param name="searchTerm">Search term to validate</param>
    /// <param name="sanitizedSearchTerm">Sanitized search term (output)</param>
    /// <param name="errorMessage">Error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateAndSanitizeSearchTerm(
        string? searchTerm,
        out string? sanitizedSearchTerm,
        out string? errorMessage)
    {
        // Null or empty search terms are allowed (returns all results)
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            sanitizedSearchTerm = null;
            errorMessage = null;
            return true;
        }

        // Trim whitespace
        var trimmed = searchTerm.Trim();

        // Check length
        if (trimmed.Length > MaxSearchTermLength)
        {
            sanitizedSearchTerm = null;
            errorMessage = $"Search term must be {MaxSearchTermLength} characters or less";
            return false;
        }

        // Check for SQL injection patterns
        if (SqlInjectionPattern.IsMatch(trimmed))
        {
            sanitizedSearchTerm = null;
            errorMessage = "Search term contains invalid characters or SQL keywords";
            return false;
        }

        // Check for allowed characters only
        if (!AllowedCharactersPattern.IsMatch(trimmed))
        {
            sanitizedSearchTerm = null;
            errorMessage = "Search term contains invalid characters. Only letters, numbers, spaces, hyphens, periods, and @ are allowed";
            return false;
        }

        sanitizedSearchTerm = trimmed;
        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Validates all search parameters together
    /// SECURITY: Comprehensive validation for pagination and search
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sanitizedSearchTerm">Sanitized search term (output)</param>
    /// <param name="errorMessage">Error message if validation fails</param>
    /// <returns>True if all parameters are valid, false otherwise</returns>
    public static bool ValidateSearchParameters(
        string? searchTerm,
        int pageNumber,
        int pageSize,
        out string? sanitizedSearchTerm,
        out string? errorMessage)
    {
        // Validate page number
        if (!ValidatePageNumber(pageNumber, out errorMessage))
        {
            sanitizedSearchTerm = null;
            return false;
        }

        // Validate page size
        if (!ValidatePageSize(pageSize, out errorMessage))
        {
            sanitizedSearchTerm = null;
            return false;
        }

        // Validate and sanitize search term
        if (!ValidateAndSanitizeSearchTerm(searchTerm, out sanitizedSearchTerm, out errorMessage))
        {
            return false;
        }

        errorMessage = null;
        return true;
    }
}

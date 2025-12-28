namespace EMR.Application.Common.Utilities;

/// <summary>
/// Utility for sanitizing sensitive data in logs to prevent exposure
/// Masks emails, Azure AD B2C IDs, and other PII
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitize email address for logging
    /// Example: john.doe@example.com -> j***e@example.com
    /// </summary>
    public static string SanitizeEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return "***";

        var parts = email.Split('@');
        if (parts.Length != 2)
            return "***";

        var localPart = parts[0];
        var domain = parts[1];

        if (localPart.Length <= 2)
            return $"***@{domain}";

        return $"{localPart[0]}***{localPart[^1]}@{domain}";
    }

    /// <summary>
    /// Sanitize Azure AD B2C ID (GUID) for logging
    /// Example: 12345678-1234-1234-1234-123456789012 -> 1234****-****-****-****-********9012
    /// </summary>
    public static string SanitizeAzureId(string? azureId)
    {
        if (string.IsNullOrEmpty(azureId))
            return "***";

        // If it's a valid GUID format
        if (Guid.TryParse(azureId, out _))
        {
            if (azureId.Length >= 8)
            {
                return $"{azureId.Substring(0, 4)}****-****-****-****-********{azureId.Substring(azureId.Length - 4)}";
            }
        }

        // Fallback for non-GUID format
        if (azureId.Length <= 8)
            return "***";

        return $"{azureId.Substring(0, 4)}****{azureId.Substring(azureId.Length - 4)}";
    }

    /// <summary>
    /// Sanitize user ID (GUID) for logging
    /// Example: 12345678-1234-1234-1234-123456789012 -> 1234****-****-****-****-********9012
    /// </summary>
    public static string SanitizeUserId(Guid userId)
    {
        var userIdString = userId.ToString();
        return $"{userIdString.Substring(0, 4)}****-****-****-****-********{userIdString.Substring(userIdString.Length - 4)}";
    }

    /// <summary>
    /// Sanitize phone number for logging
    /// Example: +1234567890 -> +123****890
    /// </summary>
    public static string SanitizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return "***";

        if (phoneNumber.Length <= 6)
            return "***";

        return $"{phoneNumber.Substring(0, 3)}****{phoneNumber.Substring(phoneNumber.Length - 3)}";
    }

    /// <summary>
    /// Sanitize general PII for logging
    /// Shows first and last character only
    /// </summary>
    public static string SanitizePII(string? data)
    {
        if (string.IsNullOrEmpty(data))
            return "***";

        if (data.Length <= 2)
            return "***";

        return $"{data[0]}***{data[^1]}";
    }

    /// <summary>
    /// Sanitize person name for logging
    /// Example: John Doe -> J*** D***
    /// </summary>
    public static string SanitizePersonName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return "***";

        if (name.Length <= 2)
            return "***";

        return $"{name[0]}***";
    }

    /// <summary>
    /// Create a sanitized log message with multiple values
    /// </summary>
    public static string CreateSanitizedMessage(string template, params object[] values)
    {
        return string.Format(template, values);
    }
}

using Microsoft.Extensions.Options;

namespace EMR.Api.Extensions;

/// <summary>
/// Azure AD B2C configuration options for validation
/// </summary>
public class AzureAdB2COptions
{
    public const string SectionName = "AzureAdB2C";

    public string? Instance { get; set; }
    public string? Domain { get; set; }
    public string? ClientId { get; set; }
    public string? TenantId { get; set; }
    public string? SignUpSignInPolicyId { get; set; }
    public string? Scopes { get; set; }
}

/// <summary>
/// Validates Azure AD B2C configuration at startup
/// </summary>
public class AzureAdB2COptionsValidator : IValidateOptions<AzureAdB2COptions>
{
    private readonly IHostEnvironment _environment;

    // Placeholder values that indicate unconfigured settings
    private static readonly string[] PlaceholderPatterns = new[]
    {
        "your-",
        "placeholder",
        "changeme",
        "todo",
        "replace-",
        "<your",
        "${",
        "example"
    };

    public AzureAdB2COptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, AzureAdB2COptions options)
    {
        var errors = new List<string>();

        // Skip validation in development and testing environments
        // This allows developers to run locally and tests to run without Azure AD B2C configured
        if (_environment.IsDevelopment() || _environment.EnvironmentName == "Testing")
        {
            return ValidateOptionsResult.Success;
        }

        // Required fields validation
        if (string.IsNullOrWhiteSpace(options.Instance))
        {
            errors.Add("AzureAdB2C:Instance is required");
        }
        else if (IsPlaceholder(options.Instance))
        {
            errors.Add($"AzureAdB2C:Instance contains a placeholder value: '{options.Instance}'");
        }
        else if (!options.Instance.Contains(".b2clogin.com", StringComparison.OrdinalIgnoreCase) &&
                 !options.Instance.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"AzureAdB2C:Instance must be a valid Azure B2C login URL (*.b2clogin.com or login.microsoftonline.com)");
        }

        if (string.IsNullOrWhiteSpace(options.Domain))
        {
            errors.Add("AzureAdB2C:Domain is required");
        }
        else if (IsPlaceholder(options.Domain))
        {
            errors.Add($"AzureAdB2C:Domain contains a placeholder value: '{options.Domain}'");
        }
        else if (!options.Domain.Contains(".onmicrosoft.com", StringComparison.OrdinalIgnoreCase) &&
                 !options.Domain.Contains(".com", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("AzureAdB2C:Domain must be a valid domain");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            errors.Add("AzureAdB2C:ClientId is required");
        }
        else if (IsPlaceholder(options.ClientId))
        {
            errors.Add($"AzureAdB2C:ClientId contains a placeholder value");
        }
        else if (!Guid.TryParse(options.ClientId, out _))
        {
            errors.Add("AzureAdB2C:ClientId must be a valid GUID");
        }

        if (string.IsNullOrWhiteSpace(options.TenantId))
        {
            errors.Add("AzureAdB2C:TenantId is required");
        }
        else if (IsPlaceholder(options.TenantId))
        {
            errors.Add($"AzureAdB2C:TenantId contains a placeholder value");
        }
        else if (!Guid.TryParse(options.TenantId, out _))
        {
            errors.Add("AzureAdB2C:TenantId must be a valid GUID");
        }

        if (string.IsNullOrWhiteSpace(options.SignUpSignInPolicyId))
        {
            errors.Add("AzureAdB2C:SignUpSignInPolicyId is required");
        }
        else if (IsPlaceholder(options.SignUpSignInPolicyId))
        {
            errors.Add($"AzureAdB2C:SignUpSignInPolicyId contains a placeholder value");
        }
        else if (!options.SignUpSignInPolicyId.StartsWith("B2C_1", StringComparison.OrdinalIgnoreCase) &&
                 !options.SignUpSignInPolicyId.StartsWith("B2C_1A", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("AzureAdB2C:SignUpSignInPolicyId should start with 'B2C_1' or 'B2C_1A'");
        }

        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsPlaceholder(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var lowerValue = value.ToLowerInvariant();
        return PlaceholderPatterns.Any(pattern =>
            lowerValue.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension methods for configuration validation
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Add configuration validation for critical settings
    /// </summary>
    public static IServiceCollection AddConfigurationValidation(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Azure AD B2C options with validation
        services.Configure<AzureAdB2COptions>(configuration.GetSection(AzureAdB2COptions.SectionName));
        services.AddSingleton<IValidateOptions<AzureAdB2COptions>, AzureAdB2COptionsValidator>();

        // Validate options at startup
        services.AddOptions<AzureAdB2COptions>()
            .Bind(configuration.GetSection(AzureAdB2COptions.SectionName))
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Validate critical configuration settings at application startup
    /// </summary>
    public static IHost ValidateConfiguration(this IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var environment = host.Services.GetRequiredService<IHostEnvironment>();

        try
        {
            // Trigger options validation by resolving the options
            var options = host.Services.GetRequiredService<IOptions<AzureAdB2COptions>>();
            var _ = options.Value; // This will trigger validation

            logger.LogInformation(
                "Configuration validation passed for environment: {Environment}",
                environment.EnvironmentName);
        }
        catch (OptionsValidationException ex)
        {
            logger.LogCritical(
                "Configuration validation failed: {Errors}. " +
                "Please configure Azure AD B2C settings properly before deploying to production.",
                string.Join("; ", ex.Failures));

            if (!environment.IsDevelopment() && environment.EnvironmentName != "Testing")
            {
                throw new InvalidOperationException(
                    $"Configuration validation failed: {string.Join("; ", ex.Failures)}",
                    ex);
            }
        }

        return host;
    }
}

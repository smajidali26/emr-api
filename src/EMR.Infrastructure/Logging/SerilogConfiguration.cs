using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace EMR.Infrastructure.Logging;

/// <summary>
/// Serilog configuration for HIPAA-compliant structured logging
/// Configures separate sinks for audit logs, application logs, and error logs
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Configure Serilog with HIPAA-compliant settings
    /// </summary>
    public static ILogger CreateLogger(string environment, string logPath = "Logs")
    {
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "EMR-API")
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithMachineName()
            .Enrich.WithThreadId();

        // Console sink for development
        if (environment == "Development")
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        // File sink for all application logs
        loggerConfiguration.WriteTo.File(
            path: Path.Combine(logPath, "application-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
            restrictedToMinimumLevel: LogEventLevel.Information);

        // Separate file sink for AUDIT logs
        // CRITICAL: These logs must be retained for 6 years for HIPAA compliance
        loggerConfiguration.WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt =>
                evt.MessageTemplate.Text.Contains("AUDIT") ||
                evt.MessageTemplate.Text.Contains("PHI_ACCESS"))
            .WriteTo.File(
                path: Path.Combine(logPath, "audit", "audit-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 2190, // 6 years retention
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}",
                restrictedToMinimumLevel: LogEventLevel.Information)
            // JSON format for SIEM integration
            .WriteTo.File(
                formatter: new CompactJsonFormatter(),
                path: Path.Combine(logPath, "audit", "audit-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 2190, // 6 years retention
                restrictedToMinimumLevel: LogEventLevel.Information));

        // Separate file sink for ERROR logs
        loggerConfiguration.WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt => evt.Level >= LogEventLevel.Error)
            .WriteTo.File(
                path: Path.Combine(logPath, "errors", "error-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90, // 90 days retention
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

        // Separate file sink for SECURITY events (failed logins, access denied, etc.)
        loggerConfiguration.WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt =>
                evt.MessageTemplate.Text.Contains("FailedLogin") ||
                evt.MessageTemplate.Text.Contains("AccessDenied") ||
                evt.MessageTemplate.Text.Contains("SECURITY"))
            .WriteTo.File(
                path: Path.Combine(logPath, "security", "security-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 365, // 1 year retention
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}"));

        return loggerConfiguration.CreateLogger();
    }

    /// <summary>
    /// Configure Serilog with external SIEM integration (e.g., Azure Monitor, Splunk)
    /// Use this in production environments
    /// </summary>
    public static ILogger CreateLoggerWithSiem(
        string environment,
        string logPath,
        string? azureWorkspaceId = null,
        string? azureWorkspaceKey = null,
        string? splunkUrl = null,
        string? splunkToken = null)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "EMR-API")
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithMachineName()
            .Enrich.WithThreadId();

        // Standard file sinks
        AddStandardSinks(loggerConfiguration, environment, logPath);

        // Azure Monitor sink for SIEM
        if (!string.IsNullOrWhiteSpace(azureWorkspaceId) && !string.IsNullOrWhiteSpace(azureWorkspaceKey))
        {
            // Note: Requires Serilog.Sinks.AzureAnalytics package
            // loggerConfiguration.WriteTo.AzureAnalytics(azureWorkspaceId, azureWorkspaceKey);
        }

        // Splunk sink for SIEM
        if (!string.IsNullOrWhiteSpace(splunkUrl) && !string.IsNullOrWhiteSpace(splunkToken))
        {
            // Note: Requires Serilog.Sinks.Splunk package
            // loggerConfiguration.WriteTo.EventCollector(splunkUrl, splunkToken);
        }

        return loggerConfiguration.CreateLogger();
    }

    private static void AddStandardSinks(LoggerConfiguration config, string environment, string logPath)
    {
        if (environment == "Development")
        {
            config.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        config.WriteTo.File(
            path: Path.Combine(logPath, "application-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        config.WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt =>
                evt.MessageTemplate.Text.Contains("AUDIT") ||
                evt.MessageTemplate.Text.Contains("PHI_ACCESS"))
            .WriteTo.File(
                formatter: new CompactJsonFormatter(),
                path: Path.Combine(logPath, "audit", "audit-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 2190));
    }
}

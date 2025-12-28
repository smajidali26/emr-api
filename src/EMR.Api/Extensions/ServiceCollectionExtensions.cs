using EMR.Application.Common.Authorization;
using EMR.Domain.Enums;
using EMR.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace EMR.Api.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure API services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Swagger/OpenAPI configuration
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen();
        return services;
    }

    /// <summary>
    /// Add CORS configuration
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Add JWT Bearer Authentication with Azure AD B2C support
    /// </summary>
    public static IServiceCollection AddAuthenticationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var azureAdB2CSection = configuration.GetSection("AzureAdB2C");

        // Configure Azure AD B2C authentication with explicit token validation
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(options =>
            {
                azureAdB2CSection.Bind(options);

                // SECURITY: Explicit token validation parameters
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;

                // SECURITY: Reduce clock skew from default 5 minutes to 30 seconds
                // to minimize token validity window after expiration
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);

                // SECURITY: Require expiration claim
                options.TokenValidationParameters.RequireExpirationTime = true;

                // SECURITY: Require signed tokens
                options.TokenValidationParameters.RequireSignedTokens = true;

                // SECURITY: Events for logging and additional validation
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerHandler>>();

                        logger.LogWarning(
                            "JWT authentication failed: {Error}. Path: {Path}",
                            context.Exception.Message,
                            context.Request.Path);

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerHandler>>();

                        var userId = context.Principal?.FindFirst("sub")?.Value
                            ?? context.Principal?.FindFirst("oid")?.Value
                            ?? "unknown";

                        logger.LogInformation(
                            "JWT token validated for user {UserId}. Path: {Path}",
                            userId,
                            context.Request.Path);

                        return Task.CompletedTask;
                    }
                };
            }, options =>
            {
                azureAdB2CSection.Bind(options);
            });

        services.AddAuthorization(options =>
        {
            // Add custom authorization policies here
            options.AddPolicy("RequireAuthenticatedUser", policy =>
            {
                policy.RequireAuthenticatedUser();
            });

            // Role-based policies for backward compatibility
            options.AddPolicy(PermissionConstants.Roles.Admin, policy =>
                policy.RequireRole(UserRole.Admin.ToString()));

            options.AddPolicy(PermissionConstants.Roles.Doctor, policy =>
                policy.RequireRole(UserRole.Doctor.ToString()));

            options.AddPolicy(PermissionConstants.Roles.Nurse, policy =>
                policy.RequireRole(UserRole.Nurse.ToString()));

            options.AddPolicy(PermissionConstants.Roles.Staff, policy =>
                policy.RequireRole(UserRole.Staff.ToString()));

            options.AddPolicy(PermissionConstants.Roles.Patient, policy =>
                policy.RequireRole(UserRole.Patient.ToString()));

            // Combined role policies
            options.AddPolicy(PermissionConstants.Roles.AdminOrDoctor, policy =>
                policy.RequireRole(UserRole.Admin.ToString(), UserRole.Doctor.ToString()));

            options.AddPolicy(PermissionConstants.Roles.AdminOrDoctorOrNurse, policy =>
                policy.RequireRole(UserRole.Admin.ToString(), UserRole.Doctor.ToString(), UserRole.Nurse.ToString()));

            options.AddPolicy(PermissionConstants.Roles.MedicalStaff, policy =>
                policy.RequireRole(UserRole.Doctor.ToString(), UserRole.Nurse.ToString()));

            // Permission-based policies - dynamically create policies for all permissions
            foreach (Permission permission in Enum.GetValues<Permission>())
            {
                var policyName = PermissionConstants.GetPolicyName(permission);
                options.AddPolicy(policyName, policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });

        return services;
    }

    /// <summary>
    /// Add Health Checks configuration
    /// </summary>
    public static IServiceCollection AddHealthCheckConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Add database health check
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddNpgSql(connectionString, name: "PostgreSQL");
        }

        // Add Redis health check
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            healthChecksBuilder.AddRedis(redisConnection, name: "Redis");
        }

        return services;
    }
}

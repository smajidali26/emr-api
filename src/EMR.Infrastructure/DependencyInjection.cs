using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using EMR.Application.Common.Interfaces;
using EMR.Domain.Interfaces;
using EMR.Infrastructure.Authorization;
using EMR.Infrastructure.Data;
using EMR.Infrastructure.Data.Interceptors;
using EMR.Infrastructure.Repositories;
using EMR.Infrastructure.Services;
using EMR.Infrastructure.TimescaleDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IAppAuthorizationService = EMR.Application.Common.Interfaces.IAuthorizationService;

namespace EMR.Infrastructure;

/// <summary>
/// Dependency injection configuration for the Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register EF Core Interceptors
        services.AddScoped<AuditInterceptor>();

        // Skip database registration in Testing environment
        // Tests will configure their own DbContext with appropriate provider (e.g., SQLite in-memory)
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment != "Testing")
        {
            // Add PostgreSQL DbContext
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                // Add audit interceptor for automatic change tracking
                var auditInterceptor = serviceProvider.GetRequiredService<AuditInterceptor>();

                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                })
                .AddInterceptors(auditInterceptor);
            });
        }

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register Specific Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IResourceAuthorizationRepository, ResourceAuthorizationRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();

        // Register Authorization Services
        services.AddScoped<IAppAuthorizationService, AuthorizationService>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ResourceAuthorizationHandler>();

        // Register Application Services
        // Note: HttpContextAccessor should be registered in the API layer
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditStatisticsService, AuditStatisticsService>();

        // Register TimescaleDB Configuration Service
        services.AddScoped<ITimescaleDbConfiguration, TimescaleDbConfiguration>();

        // Add Redis Distributed Cache
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "EMR_";
            });

            services.AddScoped<ICacheService, RedisCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Add Azure Key Vault configuration
    /// </summary>
    public static IConfigurationBuilder AddAzureKeyVault(
        this IConfigurationBuilder configuration,
        string keyVaultUrl)
    {
        if (!string.IsNullOrEmpty(keyVaultUrl))
        {
            var secretClient = new SecretClient(
                new Uri(keyVaultUrl),
                new DefaultAzureCredential());

            configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        }

        return configuration;
    }
}

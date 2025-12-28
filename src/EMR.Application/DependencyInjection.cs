using System.Reflection;
using EMR.Application.Common.Behaviours;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EMR.Application;

/// <summary>
/// Dependency injection configuration for the Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // Register AutoMapper
        services.AddAutoMapper(assembly);

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register pipeline behaviours
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLoggingBehaviour<,>));

        return services;
    }
}

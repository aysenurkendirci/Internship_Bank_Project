using Bank.Application.Common.Behaviors;
using Bank.Application.Features.Analytics;
using Bank.Application.Features.Planning;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Bank.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Logging wraps validation so rejected requests are timed and logged too.
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Read helpers shared by more than one query handler.
        services.AddScoped<ProjectionInputLoader>();
        services.AddScoped<AnalyticsHistory>();

        return services;
    }
}

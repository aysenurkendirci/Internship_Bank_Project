using Bank.Application.Abstractions.Services;
using Bank.Application.Common.Behaviors;
using Bank.Application.Services;
using Bank.Application.Features.Planning;
using Bank.Application.Services.Finance;
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

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ProjectionInputLoader>();
        services.AddScoped<ITransactionHistoryService, TransactionHistoryService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}

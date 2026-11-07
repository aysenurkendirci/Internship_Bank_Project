using Bank.Application.Abstractions.Services;
using Bank.Application.Services;
using Bank.Application.Services.Finance;
using Microsoft.Extensions.DependencyInjection;

namespace Bank.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICreditCardService, CreditCardService>();
        services.AddScoped<IRecurringCashFlowService, RecurringCashFlowService>();
        services.AddScoped<IInstallmentPlanService, InstallmentPlanService>();
        services.AddScoped<ProjectionInputLoader>();
        services.AddScoped<IProjectionService, ProjectionService>();
        services.AddScoped<IScenarioService, ScenarioService>();
        services.AddScoped<ITransactionHistoryService, TransactionHistoryService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IPersonalDataService, PersonalDataService>();

        return services;
    }
}

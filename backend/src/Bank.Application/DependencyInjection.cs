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

        return services;
    }
}

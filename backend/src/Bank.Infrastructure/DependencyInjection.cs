using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Infrastructure.Persistence;
using Bank.Infrastructure.Repositories;
using Bank.Infrastructure.Repositories.Finance;
using Bank.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BankDb")
            ?? throw new InvalidOperationException("Connection string 'BankDb' is missing.");

        services.AddSingleton(new SqlConnectionFactory(connectionString));

        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<IRecurringCashFlowRepository, RecurringCashFlowRepository>();
        services.AddScoped<IInstallmentPlanRepository, InstallmentPlanRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        // Code asks TimeProvider for the current time instead of DateTime.Now, so tests can pin it.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}

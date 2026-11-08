using Bank.Application.Abstractions.Messaging;
using Bank.Application.Abstractions.Persistence;
using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Infrastructure.Messaging;
using Bank.Infrastructure.Persistence;
using Bank.Infrastructure.Repositories;
using Bank.Infrastructure.Repositories.Finance;
using Bank.Infrastructure.Repositories.Payments;
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

        // Shared by everything in a request that takes part in a unit of work (one connection, one transaction).
        services.AddScoped<DbSession>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<IRecurringCashFlowRepository, RecurringCashFlowRepository>();
        services.AddScoped<IInstallmentPlanRepository, InstallmentPlanRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IBankCardRepository, BankCardRepository>();
        services.AddScoped<ITransactionHistoryRepository, TransactionHistoryRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IErasureRepository, ErasureRepository>();
        services.AddMemoryCache();

        AddMessaging(services, configuration);

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<ISessionPolicy, SessionPolicy>();

        // Code asks TimeProvider for the current time instead of DateTime.Now, so tests can pin it.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(MessagingOptions.Section);
        services.Configure<MessagingOptions>(section);

        // Events are always written to the outbox; whether they are forwarded to a broker is a deployment choice.
        services.AddScoped<IOutbox, OutboxWriter>();

        if (!section.GetValue<bool>(nameof(MessagingOptions.Enabled)))
            return;

        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        services.AddSingleton<OutboxProcessor>();
        services.AddHostedService<OutboxPublisherService>();
    }
}

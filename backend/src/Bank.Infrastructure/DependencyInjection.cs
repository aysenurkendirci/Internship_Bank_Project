using Bank.Application.Abstractions.Data;
using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Infrastructure.Persistence;
using Bank.Infrastructure.Repositories;
using Bank.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Veritabanı (SQLite)
        services.AddDbContext<BankDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // Repositories & Data
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Güvenlik (Security)
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        return services;
    }
}

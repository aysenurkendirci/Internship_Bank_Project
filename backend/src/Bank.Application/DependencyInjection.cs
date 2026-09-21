using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Bank.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR kurulumu: Bank.Application içindeki tüm Command ve Query'leri otomatik bulup kaydeder.
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // FluentValidation kurulumu: Gelecekte ekleyeceğimiz tüm doğrulama kurallarını otomatik bulur.
        // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}

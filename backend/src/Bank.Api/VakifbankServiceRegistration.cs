using Microsoft.Extensions.Options;
using Bank.Application.Abstractions.Investments;
using Bank.Infrastructure.Investments;
using System.Net.Http.Headers;

namespace Bank.Infrastructure.Extensions;

public static class VakifbankServiceExtensions
{
    public static IServiceCollection AddVakifbankServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<VakifbankOptions>(configuration.GetSection("Vakifbank"));

        services.AddHttpClient<IVakifbankTokenProvider, VakifbankTokenProvider>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<VakifbankOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl);
        });

        services.AddTransient<VakifbankAuthHandler>();

        services.AddHttpClient<IInvestmentsService, VakifbankInvestmentsService>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<VakifbankOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<VakifbankAuthHandler>();

        return services;
    }
}
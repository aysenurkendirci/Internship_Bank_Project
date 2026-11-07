using Bank.Application.Abstractions.Security;
using Microsoft.Extensions.Configuration;

namespace Bank.Infrastructure.Security;

public sealed class SessionPolicy : ISessionPolicy
{
    // A working day: long enough that an open session survives a lunch break, short enough that a lost device
    // is not signed in for days.
    private const int DefaultRefreshTokenHours = 8;

    public SessionPolicy(IConfiguration config)
        => RefreshTokenLifetime = TimeSpan.FromHours(config.GetValue<int?>("Session:RefreshTokenHours") ?? DefaultRefreshTokenHours);

    public TimeSpan RefreshTokenLifetime { get; }
}

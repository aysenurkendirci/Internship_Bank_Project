using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace Bank.Infrastructure.Oracle;

public sealed class OracleConnectionFactory
{
    private readonly OracleOptions _options;

    public OracleConnectionFactory(IOptions<OracleOptions> options)
    {
        _options = options.Value;
    }

    public OracleConnection CreateConnection()
        => new OracleConnection(_options.ConnectionString);
}

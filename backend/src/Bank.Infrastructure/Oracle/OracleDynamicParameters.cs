using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Bank.Infrastructure.Oracle;

public class OracleDynamicParameters : SqlMapper.IDynamicParameters
{
    private readonly DynamicParameters _dynamicParameters = new();
    private readonly List<OracleParameter> _oracleParameters = new();

    public void Add(string name, object? value = null, OracleDbType? dbType = null, ParameterDirection direction = ParameterDirection.Input)
    {
        if (dbType.HasValue)
        {
            _oracleParameters.Add(new OracleParameter(name, dbType.Value)
            {
                Direction = direction,
                Value = value
            });
        }
        else
        {
            _dynamicParameters.Add(name, value, direction: direction);
        }
    }

    public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
    {
        ((SqlMapper.IDynamicParameters)_dynamicParameters).AddParameters(command, identity);

        if (command is OracleCommand oracleCommand)
        {
            oracleCommand.BindByName = true; 
            oracleCommand.Parameters.AddRange(_oracleParameters.ToArray());
        }
    }
}

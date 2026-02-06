using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Linq;


namespace Bank.Infrastructure.Oracle;

public class OracleDynamicParameters : SqlMapper.IDynamicParameters
{
    private readonly DynamicParameters _dynamicParameters = new();
    private readonly List<OracleParameter> _oracleParameters = new();

    public void Add(
        string name,
        object? value = null,
        OracleDbType? dbType = null,
        ParameterDirection direction = ParameterDirection.Input,
        int? size = null
    )
    {
        if (dbType.HasValue)
        {
            var op = new OracleParameter(name, dbType.Value)
            {
                Direction = direction,
                Value = value ?? DBNull.Value
            };

            if (size.HasValue) op.Size = size.Value;

            _oracleParameters.Add(op);
        }
        else
        {
            _dynamicParameters.Add(name, value, direction: direction);
        }
    }

    public object? GetValue(string name)
    {
        var p = _oracleParameters.FirstOrDefault(x =>
            string.Equals(x.ParameterName, name, StringComparison.OrdinalIgnoreCase));

        if (p is null) return null;
        return p.Value == DBNull.Value ? null : p.Value;
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

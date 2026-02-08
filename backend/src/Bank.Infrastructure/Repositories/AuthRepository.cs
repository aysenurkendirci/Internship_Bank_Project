using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Auth;
using Bank.Infrastructure.Oracle;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bank.Infrastructure.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly OracleExecutor _db;

    public AuthRepository(OracleExecutor db)
    {
        _db = db;
    }

    public async Task<UserRow> GetUserByTcAsync(string tcNo)
    {
        var p = new OracleDynamicParameters();
        p.Add("P_TC_NO", tcNo, OracleDbType.Varchar2, ParameterDirection.Input);
        p.Add("O_CURSOR", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

        var result = await _db.QuerySingleAsync<UserRow>(
            "GENCBANK.PKG_AUTH.GET_USER_BY_TCN",
            p
        );

        return result ?? throw new KeyNotFoundException($"Kullanıcı bulunamadı: {tcNo}");
    }

    public async Task<UserRow?> GetUserByTcOrDefaultAsync(string tcNo)
    {
        var p = new OracleDynamicParameters();
        p.Add("P_TC_NO", tcNo, OracleDbType.Varchar2, ParameterDirection.Input);
        p.Add("O_CURSOR", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

        return await _db.QuerySingleAsync<UserRow>(
            "GENCBANK.PKG_AUTH.GET_USER_BY_TCN",
            p
        );
    }

    public async Task<CredentialRow> GetCredentialsAsync(long userId)
    {
        var p = new OracleDynamicParameters();
        p.Add("P_USER_ID", userId, OracleDbType.Int64, ParameterDirection.Input);
        p.Add("O_CURSOR", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

        var result = await _db.QuerySingleAsync<CredentialRow>(
            "GENCBANK.PKG_AUTH.GET_CREDENTIALS",
            p
        );

        return result ?? throw new KeyNotFoundException("Kimlik bilgileri bulunamadı.");
    }

    public async Task<UserRow> CreateUserAsync(RegisterRequest req, string passwordHash)
    {
        var p = new DynamicParameters();

        p.Add("P_TC_NO", req.TcNo, DbType.String, ParameterDirection.Input);
        p.Add("P_FIRST_NAME", req.FirstName, DbType.String, ParameterDirection.Input);
        p.Add("P_LAST_NAME", req.LastName, DbType.String, ParameterDirection.Input);
        p.Add("P_EMAIL", req.Email, DbType.String, ParameterDirection.Input);
        p.Add("P_PHONE", req.Phone, DbType.String, ParameterDirection.Input);
        p.Add("P_MEMBERSHIP", req.Membership, DbType.String, ParameterDirection.Input);
        p.Add("P_PASSWORD_HASH", passwordHash, DbType.String, ParameterDirection.Input);

        p.Add("O_USER_ID", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await _db.ExecuteAsync("PKG_AUTH.REGISTER_USER", p);

        var newUserId = p.Get<long>("O_USER_ID");
        if (newUserId <= 0)
            throw new InvalidOperationException("Kullanıcı oluşturuldu ama O_USER_ID dönmedi.");

        Console.WriteLine($"Yeni Kullanıcı ID: {newUserId}");
        return await GetUserByTcAsync(req.TcNo);
    }

    public async Task UpdateLastLoginAsync(long userId)
    {
        var p = new OracleDynamicParameters();
        p.Add("P_USER_ID", userId, OracleDbType.Int64, ParameterDirection.Input);

        await _db.ExecuteAsync("PKG_AUTH.UPDATE_LAST_LOGIN", p);
    }
}

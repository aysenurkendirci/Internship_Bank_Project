using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Auth;
using Bank.Infrastructure.Oracle;
using Dapper;
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
        var result = await _db.QuerySingleAsync<UserRow>(
            "PKG_AUTH.GET_USER_BY_TCN",
            new { P_TC_NO = tcNo }
        );

        return result ?? throw new KeyNotFoundException($"Kullanıcı bulunamadı: {tcNo}");
    }

    public async Task<CredentialRow> GetCredentialsAsync(long userId)
    {
        var result = await _db.QuerySingleAsync<CredentialRow>(
            "PKG_AUTH.GET_CREDENTIALS",
            new { P_USER_ID = userId }
        );

        return result ?? throw new KeyNotFoundException("Kimlik bilgileri bulunamadı.");
    }

    public async Task<UserRow> CreateUserAsync(RegisterRequest req, string passwordHash)
    {
        var p = new DynamicParameters();

        // IN parametreler (DB signature ile birebir)
        p.Add("P_TC_NO", req.TcNo, DbType.String, ParameterDirection.Input);
        p.Add("P_FIRST_NAME", req.FirstName, DbType.String, ParameterDirection.Input);
        p.Add("P_LAST_NAME", req.LastName, DbType.String, ParameterDirection.Input);
        p.Add("P_EMAIL", req.Email, DbType.String, ParameterDirection.Input);
        p.Add("P_PHONE", req.Phone, DbType.String, ParameterDirection.Input);
        p.Add("P_MEMBERSHIP", req.Membership, DbType.String, ParameterDirection.Input);
        p.Add("P_PASSWORD_HASH", passwordHash, DbType.String, ParameterDirection.Input);

        // OUT parametre
        p.Add("O_USER_ID", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await _db.ExecuteAsync("PKG_AUTH.REGISTER_USER", p);

        var userId = p.Get<long>("O_USER_ID");
        if (userId <= 0)
            throw new InvalidOperationException("Kullanıcı oluşturuldu ama O_USER_ID dönmedi.");

        // İstersen burada userId ile getiren ayrı proc varsa onu kullanabiliriz.
        // Şimdilik TC ile çekiyoruz:
        return await GetUserByTcAsync(req.TcNo);
    }

    public async Task UpdateLastLoginAsync(long userId)
    {
        await _db.ExecuteAsync(
            "PKG_AUTH.UPDATE_LAST_LOGIN",
            new { P_USER_ID = userId }
        );
    }
}

using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Auth;
using Bank.Infrastructure.Oracle;
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

    public async Task<UserRow> GetUserByIdentifierAsync(string identifier)
    {
        var result = await _db.QuerySingleAsync<UserRow>(
            "PKG_AUTH.GET_USER_BY_IDENTIFIER",
            new { p_identifier = identifier }
        );

        return result ?? throw new KeyNotFoundException($"Kullanıcı bulunamadı: {identifier}");
    }

    public async Task<CredentialRow> GetCredentialsAsync(long userId)
    {
        var result = await _db.QuerySingleAsync<CredentialRow>(
            "PKG_AUTH.GET_CREDENTIALS",
            new { p_user_id = userId }
        );

        return result ?? throw new KeyNotFoundException("Kimlik bilgileri bulunamadı.");
    }

    public async Task<UserRow> CreateUserAsync(RegisterRequest req, string passwordHash)
    {
        // ✅ 1) Önce procedure ile INSERT/REGISTER işlemini yap
        await _db.ExecuteAsync(
            "PKG_AUTH.REGISTER_USER",
            new
            {
                p_tc_no = req.TcNo,
                p_first_name = req.FirstName,
                p_last_name = req.LastName,
                p_email = req.Email,
                p_phone = req.Phone,
                p_membership = req.Membership,
                p_password_hash = passwordHash
            }
        );

        // ✅ 2) Sonra kayıt edilen kullanıcıyı tekrar çek (procedure satır döndürmüyor olabilir)
        // Identifier parametren email veya tcno ise ona göre seç.
        // En mantıklısı: email ile çekmek.
        var user = await GetUserByIdentifierAsync(req.Email);

        return user;
    }

    public async Task UpdateLastLoginAsync(long userId)
    {
        await _db.ExecuteAsync(
            "PKG_AUTH.UPDATE_LAST_LOGIN",
            new { p_user_id = userId }
        );
    }
}

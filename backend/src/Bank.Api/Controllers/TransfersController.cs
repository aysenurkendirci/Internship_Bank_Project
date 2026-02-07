using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Bank.Infrastructure.Oracle; // OracleExecutor ve OracleDynamicParameters burada tanımlı

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/transfers")]
public sealed class TransfersController : ControllerBase
{
    private readonly OracleExecutor _db;

    public TransfersController(OracleExecutor db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransferRequest req)
    {
        // Kullanıcı ID'sini claim'lerden güvenli bir şekilde alıyoruz
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                        User.FindFirstValue("userId");

        if (!long.TryParse(userIdStr, out var userId))
            return Unauthorized(new { Message = "Kullanıcı kimliği bulunamadı." });

        var p = new OracleDynamicParameters();
        p.Add("p_from_account_id", req.FromAccountId, OracleDbType.Int64, ParameterDirection.Input);
        p.Add("p_to_account_id", req.ToAccountId, OracleDbType.Int64, ParameterDirection.Input);
        p.Add("p_to_card_id", req.ToCardId, OracleDbType.Int64, ParameterDirection.Input);
        p.Add("p_amount", req.Amount, OracleDbType.Decimal, ParameterDirection.Input);
        p.Add("p_note", req.Note, OracleDbType.Varchar2, ParameterDirection.Input);
        p.Add("o_status", dbType: OracleDbType.Varchar2, direction: ParameterDirection.Output, size: 50);

        // Paket içindeki prosedürü çağır
        await _db.ExecuteAsync("GENCBANK.PKG_TRANSFERS.CREATE_TRANSFER", p);

        // OUT parametresini yakala
        var status = p.GetValue("o_status")?.ToString();

        return status switch
        {
            "SUCCESS" => Ok(new { Message = "İşlem başarıyla tamamlandı." }),
            "INSUFFICIENT_FUNDS" => BadRequest(new { Message = "Bakiye yetersiz." }),
            "NOT_FOUND" => NotFound(new { Message = "Hesap veya kart bulunamadı." }),
            _ => StatusCode(500, new { Message = "Bir hata oluştu: " + status })
        };
    }
}

public record TransferRequest(
    long FromAccountId, 
    long? ToAccountId, 
    long? ToCardId, 
    decimal Amount, 
    string? Note
);
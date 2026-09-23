using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Dashboard;
using Bank.Domain.Exceptions;
using MediatR;

namespace Bank.Application.Features.Dashboard.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    private readonly IUserRepository _userRepository;

    public GetDashboardQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        // 1. Veritabanından kullanıcıyı ve bağlı tüm ilişkili verileri (Hesaplar, Kartlar, İşlemler) getir.
        var user = await _userRepository.GetByIdWithDetailsAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new DomainException("Kullanıcı bulunamadı.");
        }

        // 2. DTO Dönüşümü (Domain'den Contracts'a)
        var userSummary = new UserSummary(user.Id, user.FirstName, user.MembershipTier);
        
        decimal totalWealth = user.Accounts.Sum(a => a.Balance);
        // İleride buraya WealthChangeRate (örn. bir önceki aya göre) hesaplaması eklenebilir. Şimdilik 0.
        decimal wealthChangeRate = 0m; 

        var accountsList = user.Accounts.Select(a => new AccountItem(
            a.Id,
            "Vadesiz Hesap", // Şimdilik statik
            a.AccountNumber,
            a.Balance,
            "Aktif",
            $"IBAN: TR00 0000 0000 {a.AccountNumber}",
            "wallet"
        )).ToList();

        var cardsList = user.Accounts.SelectMany(a => a.Cards).Select(c => new CardItem(
            c.Id,
            $"**** **** **** {c.CardNumber[^4..]}",
            c.Type.ToString(),
            c.IsVirtual,
            c.IsActive ? "Aktif" : "Pasif",
            c.Account.Balance, // Kartın bağlı olduğu hesabın bakiyesi
            new CardSettings(c.IsContactlessEnabled, c.IsOnlineEnabled),
            new CardLimits(c.DailyLimit, c.MonthlyLimit)
        )).ToList();

        var transactionsList = user.Accounts.SelectMany(a => a.LedgerEntries)
            .Select(le => le.Transaction)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new TransactionItem(
                t.Id,
                t.Description,
                t.Type.ToString(),
                t.Amount,
                t.Amount < 0 ? "out" : "in",
                t.CreatedAt
            )).ToList();

        // 3. Menüyü hazırla ve dön
        return new DashboardResponse(
            userSummary,
            totalWealth,
            wealthChangeRate,
            cardsList,
            transactionsList,
            accountsList
        );
    }
}

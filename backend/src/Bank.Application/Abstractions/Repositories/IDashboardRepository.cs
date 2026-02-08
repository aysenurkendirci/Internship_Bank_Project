using Bank.Contracts.Dashboard;

namespace Bank.Application.Abstractions.Repositories;

public interface IDashboardRepository
{
    Task<DashboardResponse> GetDashboardAsync(long userId, CancellationToken ct = default);

    Task<IReadOnlyList<SavingsGoalRow>> GetSavingsGoalsAsync(long userId, CancellationToken ct = default);

    Task CreateSavingsGoalAsync(long userId, CreateSavingsGoalRequest req, CancellationToken ct = default);
}

/// <summary>
/// DB’den dönen Savings Goal satırı (mapping için).
/// Infrastructure repository bunu doldurur, Service bunu DTO’ya çevirir.
/// </summary>
public sealed class SavingsGoalRow
{
    public long GOAL_ID { get; set; }
    public string TITLE { get; set; } = "";
    public decimal TARGET_AMOUNT { get; set; }
    public decimal CURRENT_AMOUNT { get; set; }
    public decimal PROGRESS_PERCENT { get; set; }
    public string STATUS { get; set; } = "";
    public DateTime CREATED_AT { get; set; }
}

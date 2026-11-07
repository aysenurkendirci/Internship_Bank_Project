using Bank.Contracts.Finance;
using Bank.Domain.Projection;

namespace Bank.Application.Features.Planning;

/// <summary>Domain → contract mapping shared by the projection and scenario endpoints.</summary>
internal static class ProjectionMapper
{
    public static ProjectionMonthResponse ToResponse(MonthlyCashFlow m)
        => new(
            m.Year,
            m.Month,
            m.Income.Amount,
            m.Outflows.Amount,
            m.CardPayments.Amount,
            m.Net.Amount,
            m.ClosingBalance.Amount,
            m.LowestBalance.Amount,
            m.LowestBalanceDate,
            m.BurdenRatio,
            m.IsDeficit,
            m.BalanceGoesNegative);

    public static ProjectionEntryResponse ToResponse(ProjectionEntry e)
        => new(
            e.Date,
            e.Description,
            e.Kind.ToString(),
            e.Amount.Amount,
            e.CreditCardId,
            e.StatementDate,
            e.Charges.Select(c => new CardChargeResponse(c.Description, c.Amount.Amount)).ToList());
}

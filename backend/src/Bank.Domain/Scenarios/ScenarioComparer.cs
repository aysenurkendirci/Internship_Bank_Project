using Bank.Domain.Cards;
using Bank.Domain.Common;
using Bank.Domain.Projection;

namespace Bank.Domain.Scenarios;

/// <summary>
/// Answers "what if I bought this in N installments instead of M?" by running the projection once
/// without the purchase and once per option with a hypothetical installment plan added.
/// </summary>
/// <remarks>
/// All options are compared over the same horizon, stretched until the last installment of the
/// longest option: comparing a 3-installment and a 12-installment plan over six months would hide
/// half of the longer plan's cost.
/// The result states facts per option; it deliberately does not rank options or recommend one.
/// </remarks>
public static class ScenarioComparer
{
    public const int MaxOptions = 6;

    /// <param name="current">The user's current situation; its <c>Months</c> is the minimum horizon.</param>
    public static ScenarioComparison Compare(ProjectionInput current, PurchaseScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(scenario);

        var options = scenario.Options;
        if (options.Count is < 1 or > MaxOptions)
            throw new DomainException($"1 ile {MaxOptions} arasında seçenek karşılaştırılabilir.");
        if (options.Distinct().Count() != options.Count)
            throw new DomainException("Aynı seçenek birden fazla kez verilmiş.");
        if (scenario.PurchaseDate < current.From)
            throw new DomainException("Alışveriş tarihi geçmişte olamaz.");

        var card = current.Cards.SingleOrDefault(c => c.Id == scenario.CreditCardId)
                   ?? throw new DomainException("Senaryodaki kart bulunamadı.");
        if (card.Limit.Currency != current.OpeningBalance.Currency)
            throw new DomainException("Senaryo yalnızca projeksiyon para birimindeki bir kartla yapılabilir.");

        var plans = options
            .Select(o => new InstallmentPlan(0, card.Id, scenario.Description, o.TotalAmount, o.InstallmentCount, scenario.PurchaseDate))
            .ToList();
        var schedules = plans.Select(p => p.ScheduleOn(card)).ToList();

        var horizon = current with { Months = HorizonMonths(current, schedules) };
        var baseline = CashFlowProjector.Project(horizon);

        var outcomes = plans
            .Select((plan, i) => new OptionOutcome(
                options[i],
                schedules[i],
                ScenarioMetrics.From(CashFlowProjector.Project(
                    horizon with { InstallmentPlans = [.. horizon.InstallmentPlans, plan] }))))
            .ToList();

        return new ScenarioComparison(baseline.From, baseline.To, ScenarioMetrics.From(baseline), outcomes);
    }

    private static int HorizonMonths(ProjectionInput current, IEnumerable<IReadOnlyList<Installment>> schedules)
    {
        var lastDueDate = schedules.Max(s => s[^1].DueDate);
        var monthsUntilLastDue = (lastDueDate.Year - current.From.Year) * 12 + lastDueDate.Month - current.From.Month + 1;
        var months = Math.Max(current.Months, monthsUntilLastDue);

        if (months > CashFlowProjector.MaxMonths)
            throw new DomainException(
                $"En uzun seçenek {CashFlowProjector.MaxMonths} aylık karşılaştırma süresini aşıyor.");

        return months;
    }
}

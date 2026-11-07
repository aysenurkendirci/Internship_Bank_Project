using Bank.Domain.Cards;
using Bank.Domain.CashFlows;
using Bank.Domain.Common;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Projection;

/// <summary>
/// Turns the user's recurring items, cards and installment plans into a dated cash-flow timeline
/// and monthly summaries. Pure: same input, same output; no clock, no I/O.
/// </summary>
/// <remarks>
/// Card spending does not hit the account on the day it happens. Every card charge (installments and
/// card-paid recurring items) is grouped by statement and appears once, as a single card payment on
/// the statement's due date. Assumptions are recorded in docs/adr/0003.
/// </remarks>
public static class CashFlowProjector
{
    public const int MaxMonths = 24;

    // A charge reaches the account at most about two months after it is made: up to a month until
    // the statement closes, up to 30 days until it is due, plus a weekend shift. Looking three
    // months back guarantees that charges made before the horizon but due inside it are included.
    private const int CardChargeLookbackMonths = 3;

    public static CashFlowProjection Project(ProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Months is < 1 or > MaxMonths)
            throw new DomainException($"Projeksiyon süresi 1 ile {MaxMonths} ay arasında olmalıdır.");

        var currency = input.OpeningBalance.Currency;
        var from = input.From;
        var to = from.AddMonths(input.Months).AddDays(-1);
        var excluded = new List<string>();

        var entries = DirectEntries(input.CashFlows, currency, from, to, excluded)
            .Concat(CardPaymentEntries(input, currency, from, to, excluded))
            // Same-day outflows are applied before income: the lowest balance is reported
            // conservatively rather than assuming the salary always arrives first.
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Amount.Amount)
            .ToList();

        var months = Summarise(entries, from, to, input.OpeningBalance);

        return new CashFlowProjection(from, to, input.OpeningBalance, entries, months, excluded);
    }

    private static IEnumerable<ProjectionEntry> DirectEntries(
        IEnumerable<RecurringCashFlow> cashFlows, Currency currency, DateOnly from, DateOnly to, List<string> excluded)
    {
        foreach (var flow in cashFlows.Where(f => !f.IsChargedToCard))
        {
            if (flow.Amount.Currency != currency)
            {
                excluded.Add(flow.Name);
                continue;
            }

            foreach (var occurrence in flow.OccurrencesBetween(from, to))
                yield return new ProjectionEntry(
                    occurrence.Date, occurrence.Name, ToEntryKind(occurrence.Kind), occurrence.NetAmount,
                    CreditCardId: null, StatementDate: null, Charges: []);
        }
    }

    private static IEnumerable<ProjectionEntry> CardPaymentEntries(
        ProjectionInput input, Currency currency, DateOnly from, DateOnly to, List<string> excluded)
    {
        var lookbackFrom = from.AddMonths(-CardChargeLookbackMonths);
        var cards = input.Cards.ToDictionary(c => c.Id);

        var cardFlows = input.CashFlows.Where(f => f.IsChargedToCard).ToList();
        EnsureCardsKnown(cardFlows.Select(f => f.CreditCardId!.Value), cards);
        EnsureCardsKnown(input.InstallmentPlans.Select(p => p.CreditCardId), cards);

        foreach (var card in input.Cards)
        {
            var flowsOnCard = cardFlows.Where(f => f.CreditCardId == card.Id).ToList();
            var plansOnCard = input.InstallmentPlans.Where(p => p.CreditCardId == card.Id).ToList();

            if (card.Limit.Currency != currency)
            {
                excluded.AddRange(flowsOnCard.Select(f => f.Name));
                excluded.AddRange(plansOnCard.Select(p => p.Description));
                continue;
            }

            var recurringCharges = flowsOnCard
                .SelectMany(f => f.OccurrencesBetween(lookbackFrom, to))
                .Select(o => (Statement: card.StatementDateFor(o.Date), Charge: new CardCharge(o.Name, -o.NetAmount)));

            var installmentCharges = plansOnCard
                .SelectMany(p => p.ScheduleOn(card).Select(i => (
                    Statement: i.StatementDate,
                    Charge: new CardCharge($"{p.Description} ({i.Number}/{p.InstallmentCount})", i.Amount))));

            var statements = recurringCharges
                .Concat(installmentCharges)
                .GroupBy(x => x.Statement)
                .OrderBy(g => g.Key);

            foreach (var statement in statements)
            {
                var dueDate = card.DueDateFor(statement.Key);
                if (dueDate < from || dueDate > to)
                    continue;

                var charges = statement.Select(x => x.Charge).ToList();
                var total = charges.Aggregate(Money.Zero(currency), (sum, c) => sum + c.Amount);

                yield return new ProjectionEntry(
                    dueDate, $"{card.Name} ekstresi", ProjectionEntryKind.CardPayment, -total,
                    card.Id, statement.Key, charges);
            }
        }
    }

    private static List<MonthlyCashFlow> Summarise(
        IReadOnlyList<ProjectionEntry> entries, DateOnly from, DateOnly to, Money openingBalance)
    {
        var currency = openingBalance.Currency;
        var zero = Money.Zero(currency);
        var balance = openingBalance;
        var months = new List<MonthlyCashFlow>();

        for (var month = new DateOnly(from.Year, from.Month, 1); month <= to; month = month.AddMonths(1))
        {
            var inMonth = entries.Where(e => e.Date.Year == month.Year && e.Date.Month == month.Month).ToList();

            var income = inMonth.Where(e => e.Amount.IsPositive).Aggregate(zero, (s, e) => s + e.Amount);
            var outflows = inMonth.Where(e => e.Amount.IsNegative).Aggregate(zero, (s, e) => s - e.Amount);
            var cardPayments = inMonth.Where(e => e.Kind == ProjectionEntryKind.CardPayment)
                                      .Aggregate(zero, (s, e) => s - e.Amount);

            var lowest = balance;
            DateOnly? lowestDate = null;
            foreach (var entry in inMonth)
            {
                balance += entry.Amount;
                if (balance < lowest)
                {
                    lowest = balance;
                    lowestDate = entry.Date;
                }
            }

            decimal? burden = income.IsPositive
                ? Math.Round(outflows.Amount / income.Amount, 4, MidpointRounding.AwayFromZero)
                : null;

            months.Add(new MonthlyCashFlow(
                month.Year, month.Month, income, outflows, cardPayments, income - outflows,
                balance, lowest, lowestDate, burden));
        }

        return months;
    }

    private static void EnsureCardsKnown(IEnumerable<long> cardIds, IReadOnlyDictionary<long, CreditCard> cards)
    {
        var missing = cardIds.FirstOrDefault(id => !cards.ContainsKey(id), defaultValue: -1);
        if (missing != -1)
            throw new DomainException($"Projeksiyon girdisinde {missing} numaralı kart eksik.");
    }

    private static ProjectionEntryKind ToEntryKind(CashFlowKind kind) => kind switch
    {
        CashFlowKind.Income => ProjectionEntryKind.Income,
        CashFlowKind.Expense => ProjectionEntryKind.Expense,
        CashFlowKind.Subscription => ProjectionEntryKind.Subscription,
        _ => throw new InvalidOperationException($"Unhandled cash flow kind {kind}.")
    };
}

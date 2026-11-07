import { ScenarioComparison, ScenarioMetrics } from '../../data-access/models/finance.models';
import { toComparisonRows } from './comparison-rows';

const metrics = (lowestBalance: number): ScenarioMetrics => ({
  lowestBalance,
  lowestBalanceDate: null,
  monthsBelowZero: lowestBalance < 0 ? 1 : 0,
  deficitMonths: 0,
  peakBurdenRatio: null,
  peakMonthlyOutflow: 0,
  closingBalance: 0,
  months: [],
});

const installments = (count: number, amount: number) =>
  Array.from({ length: count }, (_, i) => ({ number: i + 1, statementDate: '2026-04-10', dueDate: '2026-04-20', amount }));

const comparison: ScenarioComparison = {
  from: '2026-04-01',
  to: '2026-09-30',
  baseline: metrics(1000),
  options: [
    // Entered as 6 first, then 3: the better-looking option is deliberately second.
    { installmentCount: 6, totalAmount: 9450, installments: installments(6, 1575), metrics: metrics(500) },
    { installmentCount: 3, totalAmount: 9000, installments: installments(3, 3000), metrics: metrics(-3200) },
  ],
};

describe('toComparisonRows', () => {
  it('puts the baseline first and keeps the options in the order entered', () => {
    expect(toComparisonRows(comparison).map((r) => r.label)).toEqual(['Almasam', '6 taksit', '3 taksit']);
  });

  it('measures each price against the cheapest option', () => {
    expect(toComparisonRows(comparison).map((r) => r.extraCost)).toEqual([null, 450, 0]);
  });

  it('reports the regular installment, not the first one carrying the rounding remainder', () => {
    const withRemainder: ScenarioComparison = {
      ...comparison,
      options: [
        {
          installmentCount: 3,
          totalAmount: 1000,
          installments: [
            { number: 1, statementDate: '', dueDate: '', amount: 333.34 },
            { number: 2, statementDate: '', dueDate: '', amount: 333.33 },
            { number: 3, statementDate: '', dueDate: '', amount: 333.33 },
          ],
          metrics: metrics(0),
        },
      ],
    };

    expect(toComparisonRows(withRemainder)[1].monthlyInstallment).toBe(333.33);
  });
});

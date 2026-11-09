import { ScenarioComparison, ScenarioMetrics } from '../../data-access/models/finance.models';
import { CHART } from '../../shared/chart/chart-theme';

// Some series colours sit below 3:1 on white, so the table swatch, legend text and line-end labels carry
// identity alongside colour.

export interface ComparisonRow {
  label: string;
  colour: string;
  monthlyInstallment: number | null;
  totalAmount: number | null;
  /** Difference to the cheapest option's total; null for the no-purchase baseline. */
  extraCost: number | null;
  metrics: ScenarioMetrics;
}

/**
 * The baseline first, then one row per option in the order the user entered them. Rows are deliberately
 * never sorted by any notion of "best": the comparison informs the decision, it does not make it.
 */
export function toComparisonRows(result: ScenarioComparison): ComparisonRow[] {
  const cheapest = Math.min(...result.options.map((o) => o.totalAmount));

  return [
    { label: 'Almasam', colour: CHART.baseline, monthlyInstallment: null, totalAmount: null, extraCost: null, metrics: result.baseline },
    ...result.options.map((option, i) => ({
      label: `${option.installmentCount} taksit`,
      colour: CHART.series[i],
      // The last installment is the regular amount; any rounding remainder sits on the first one.
      monthlyInstallment: option.installments[option.installments.length - 1]?.amount ?? null,
      totalAmount: option.totalAmount,
      extraCost: Math.round((option.totalAmount - cheapest) * 100) / 100,
      metrics: option.metrics,
    })),
  ];
}

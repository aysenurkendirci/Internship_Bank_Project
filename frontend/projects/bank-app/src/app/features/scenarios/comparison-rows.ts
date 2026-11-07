import { ScenarioComparison, ScenarioMetrics } from '../../data-access/models/finance.models';

// Validated categorical order (see chart-theme.ts); three of these sit below 3:1 on white, so the table
// swatch, legend text and line-end labels carry identity alongside colour.
const OPTION_COLOURS = ['#2a78d6', '#eb6834', '#1baf7a', '#eda100', '#e87ba4', '#008300'];
const BASELINE_COLOUR = '#94a3b8';

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
    { label: 'Almasam', colour: BASELINE_COLOUR, monthlyInstallment: null, totalAmount: null, extraCost: null, metrics: result.baseline },
    ...result.options.map((option, i) => ({
      label: `${option.installmentCount} taksit`,
      colour: OPTION_COLOURS[i],
      // The last installment is the regular amount; any rounding remainder sits on the first one.
      monthlyInstallment: option.installments[option.installments.length - 1]?.amount ?? null,
      totalAmount: option.totalAmount,
      extraCost: Math.round((option.totalAmount - cheapest) * 100) / 100,
      metrics: option.metrics,
    })),
  ];
}

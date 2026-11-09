import tokens from '../../../design/tokens.json';

// Chart colours by role, read from the same file Tailwind uses. Series slots follow the validated categorical
// order. Status red is reserved for "below zero" and always appears with an icon and text, never alone.
export const CHART = {
  series: tokens.chart.series,
  series1: tokens.chart.series[0],
  series2: tokens.chart.series[1],
  baseline: tokens.chart.baseline,
  critical: tokens.chart.critical,
  textPrimary: tokens.color.ink.DEFAULT,
  textSecondary: tokens.color.ink.faint,
  grid: tokens.color.line,
  surface: tokens.color.surface,
  font: "'Inter', system-ui, sans-serif",
} as const;

const tryFormatter = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 0 });
const tryCompact = new Intl.NumberFormat('tr-TR', { notation: 'compact', maximumFractionDigits: 1 });

export const formatTry = (value: number): string => tryFormatter.format(value);

/** Axis labels: "45 B" instead of "45.000,00 ₺" keeps the axis narrow; tooltips show the full amount. */
export const formatTryCompact = (value: number): string => tryCompact.format(value);

/** Recessive axes and gridlines shared by every cartesian chart. */
export const baseAxisStyle = {
  axisLine: { lineStyle: { color: CHART.grid } },
  axisTick: { show: false },
  axisLabel: { color: CHART.textSecondary, fontFamily: CHART.font, fontSize: 11 },
  splitLine: { lineStyle: { color: CHART.grid, width: 1, type: 'solid' as const } },
};

export const baseTooltip = {
  backgroundColor: CHART.surface,
  borderColor: CHART.grid,
  textStyle: { color: CHART.textPrimary, fontFamily: CHART.font, fontSize: 12 },
};

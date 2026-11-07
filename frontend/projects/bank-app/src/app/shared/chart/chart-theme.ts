// Chart colours by role. Series slots follow the validated categorical order (blue, then orange):
// CVD ΔE 24.7 and normal-vision ΔE 33.6 between them, both ≥ 3:1 against the white panel.
// Status red is reserved for "below zero" and always appears with an icon and text, never alone.
export const CHART = {
  series1: '#2a78d6',
  series2: '#eb6834',
  critical: '#e34948',
  textPrimary: '#0f172a',
  textSecondary: '#64748b',
  grid: '#e2e8f0',
  surface: '#ffffff',
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

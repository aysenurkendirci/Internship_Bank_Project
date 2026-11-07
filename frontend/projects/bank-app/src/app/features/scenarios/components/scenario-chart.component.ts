import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { EChartsCoreOption } from 'echarts/core';

import { formatMonth } from '../../../core/util/dates';
import { ChartComponent } from '../../../shared/chart/chart.component';
import { CHART, baseAxisStyle, baseTooltip, formatTry, formatTryCompact } from '../../../shared/chart/chart-theme';
import { ComparisonRow } from '../comparison-rows';

/** Each option's lowest balance per month, against a zero line. */
@Component({
  selector: 'app-scenario-chart',
  imports: [ChartComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `<app-chart class="h-72" [option]="option()" label="Seçeneklere göre aylık en düşük bakiye. Değerler karşılaştırma tablosunda." />`,
})
export class ScenarioChartComponent {
  readonly rows = input.required<readonly ComparisonRow[]>();

  protected readonly option = computed<EChartsCoreOption>(() => {
    const rows = this.rows();
    // Up to four series are also labelled at their line end, so identity never depends on colour alone.
    const labelEnds = rows.length <= 4;

    return {
      textStyle: { fontFamily: CHART.font },
      legend: { top: 0, left: 0, icon: 'roundRect', itemWidth: 12, itemHeight: 12, textStyle: { color: CHART.textSecondary } },
      grid: { left: 8, right: labelEnds ? 72 : 16, top: 40, bottom: 8, containLabel: true },
      tooltip: { ...baseTooltip, trigger: 'axis', valueFormatter: (v: number) => formatTry(v) },
      xAxis: {
        ...baseAxisStyle,
        type: 'category',
        data: rows[0]?.metrics.months.map((m) => formatMonth(m)) ?? [],
        boundaryGap: false,
        splitLine: { show: false },
      },
      yAxis: { ...baseAxisStyle, type: 'value', axisLabel: { ...baseAxisStyle.axisLabel, formatter: formatTryCompact } },
      series: rows.map((row, i) => ({
        name: row.label,
        type: 'line',
        symbol: 'circle',
        symbolSize: 8,
        lineStyle: { width: 2, color: row.colour },
        itemStyle: { color: row.colour, borderColor: CHART.surface, borderWidth: 2 },
        endLabel: { show: labelEnds, formatter: '{a}', color: CHART.textSecondary, fontFamily: CHART.font },
        data: row.metrics.months.map((m) => m.lowestBalance),
        ...(i === 0
          ? {
              markLine: {
                symbol: 'none',
                silent: true,
                lineStyle: { color: CHART.critical, width: 1, type: 'solid' },
                label: { formatter: '0 ₺', color: CHART.textSecondary, position: 'insideStartTop' },
                data: [{ yAxis: 0 }],
              },
            }
          : {}),
      })),
    };
  });
}

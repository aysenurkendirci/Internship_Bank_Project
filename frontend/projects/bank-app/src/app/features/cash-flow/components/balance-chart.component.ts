import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { EChartsCoreOption } from 'echarts/core';

import { Projection } from '../../../data-access/models/finance.models';
import { ChartComponent } from '../../../shared/chart/chart.component';
import { CHART, baseAxisStyle, baseTooltip, formatDateTick, formatTry, formatTryCompact } from '../../../shared/chart/chart-theme';
import { runningBalance } from '../running-balance';

const dayName = new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' });

/** Day-by-day balance as a single step line, with zero marked: below it the account is overdrawn. */
@Component({
  selector: 'app-balance-chart',
  imports: [ChartComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `<app-chart class="h-72" [option]="option()" [label]="summary()" />`,
})
export class BalanceChartComponent {
  readonly projection = input.required<Projection>();

  private readonly points = computed(() => {
    const p = this.projection();
    return runningBalance(p.openingBalance, p.from, p.entries);
  });

  protected readonly summary = computed(() => {
    const lowest = this.points().reduce((low, point) => (point.balance < low.balance ? point : low));
    return `Gün gün tahmini bakiye. En düşük bakiye ${formatTry(lowest.balance)}, ${dayName.format(new Date(lowest.date + 'T00:00'))}.`;
  });

  protected readonly option = computed<EChartsCoreOption>(() => ({
    textStyle: { fontFamily: CHART.font },
    grid: { left: 8, right: 16, top: 24, bottom: 8, containLabel: true },
    tooltip: {
      ...baseTooltip,
      trigger: 'axis',
      axisPointer: { type: 'line', lineStyle: { color: CHART.textSecondary } },
      valueFormatter: (value: number) => formatTry(value),
    },
    xAxis: { ...baseAxisStyle, type: 'time', axisLabel: { ...baseAxisStyle.axisLabel, formatter: formatDateTick }, splitLine: { show: false } },
    yAxis: { ...baseAxisStyle, type: 'value', axisLabel: { ...baseAxisStyle.axisLabel, formatter: formatTryCompact } },
    series: [
      {
        name: 'Bakiye',
        type: 'line',
        step: 'end',
        symbol: 'none',
        lineStyle: { width: 2, color: CHART.series1 },
        areaStyle: { color: CHART.series1, opacity: 0.1 },
        data: this.points().map((point) => [point.date, point.balance]),
        markLine: {
          symbol: 'none',
          silent: true,
          lineStyle: { color: CHART.critical, width: 1, type: 'solid' },
          label: { formatter: '0 ₺', color: CHART.textSecondary, position: 'insideStartTop' },
          data: [{ yAxis: 0 }],
        },
      },
    ],
  }));
}

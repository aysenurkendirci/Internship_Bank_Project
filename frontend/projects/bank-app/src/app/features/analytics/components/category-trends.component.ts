import { CurrencyPipe, PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { EChartsCoreOption } from 'echarts/core';

import { CategoryTrends } from '../../../data-access/models/finance.models';
import { ChartComponent } from '../../../shared/chart/chart.component';
import { CHART, baseAxisStyle, baseTooltip, formatTry, formatTryCompact } from '../../../shared/chart/chart-theme';
import { EmptyStateComponent } from '../../../shared/ui/empty-state.component';

const monthName = new Intl.DateTimeFormat('tr-TR', { month: 'short', year: 'numeric' });

/** Spending per category: a bar chart of totals and a month-by-month table that doubles as its accessible view. */
@Component({
  selector: 'app-category-trends',
  imports: [ChartComponent, EmptyStateComponent, CurrencyPipe, PercentPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './category-trends.component.html',
})
export class CategoryTrendsComponent {
  readonly trends = input.required<CategoryTrends>();

  protected readonly monthLabels = computed(() =>
    this.trends().months.map((m) => {
      const [year, month] = m.split('-').map(Number);
      return monthName.format(new Date(year, month - 1, 1));
    }),
  );

  protected readonly chart = computed<EChartsCoreOption | null>(() => {
    const categories = this.trends().categories;
    if (categories.length === 0) return null;

    const bottomUp = [...categories].reverse(); // horizontal bars are drawn from the bottom
    return {
      textStyle: { fontFamily: CHART.font },
      grid: { left: 8, right: 24, top: 8, bottom: 8, containLabel: true },
      tooltip: { ...baseTooltip, trigger: 'item', valueFormatter: (v: number) => formatTry(v) },
      xAxis: { ...baseAxisStyle, type: 'value', axisLabel: { ...baseAxisStyle.axisLabel, formatter: formatTryCompact } },
      yAxis: { ...baseAxisStyle, type: 'category', data: bottomUp.map((c) => c.category), splitLine: { show: false } },
      series: [
        {
          name: 'Toplam harcama',
          type: 'bar',
          barMaxWidth: 20,
          itemStyle: { color: CHART.series1, borderRadius: [0, 4, 4, 0] },
          data: bottomUp.map((c) => c.total),
        },
      ],
    };
  });
}

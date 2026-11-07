import { CurrencyPipe, DatePipe, NgClass, PercentPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { EChartsCoreOption } from 'echarts/core';

import { problemMessage } from '../../core/http/problem-details';
import { PlanningApi } from '../../data-access/api/finance.api';
import { Projection, ProjectionEntryKind, ProjectionMonth } from '../../data-access/models/finance.models';
import { ChartComponent } from '../../shared/chart/chart.component';
import { CHART, baseAxisStyle, baseTooltip, formatTry, formatTryCompact } from '../../shared/chart/chart-theme';
import { runningBalance } from './running-balance';

const monthName = new Intl.DateTimeFormat('tr-TR', { month: 'short', year: 'numeric' });
const dayName = new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' });

export const ENTRY_KIND_LABELS: Record<ProjectionEntryKind, string> = {
  Income: 'Gelir',
  Expense: 'Gider',
  Subscription: 'Abonelik',
  CardPayment: 'Kart ekstresi',
};

@Component({
  selector: 'app-cash-flow-page',
  imports: [ChartComponent, CurrencyPipe, DatePipe, NgClass, PercentPipe],
  templateUrl: './cash-flow-page.component.html',
})
export class CashFlowPageComponent {
  private readonly api = inject(PlanningApi);

  protected readonly horizons = [3, 6, 12];
  protected readonly kindLabels = ENTRY_KIND_LABELS;

  protected readonly months = signal(6);
  protected readonly projection = signal<Projection | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly openStatement = signal<string | null>(null);

  protected readonly lowest = computed(() => {
    const months = this.projection()?.months ?? [];
    return months.length === 0 ? null : months.reduce((low, m) => (m.lowestBalance < low.lowestBalance ? m : low));
  });

  protected readonly monthsBelowZero = computed(() => this.projection()?.months.filter((m) => m.balanceGoesNegative).length ?? 0);

  protected readonly balanceChart = computed<EChartsCoreOption | null>(() => {
    const p = this.projection();
    if (!p) return null;
    const points = runningBalance(p.openingBalance, p.from, p.entries);

    return {
      textStyle: { fontFamily: CHART.font },
      grid: { left: 8, right: 16, top: 24, bottom: 8, containLabel: true },
      tooltip: {
        ...baseTooltip,
        trigger: 'axis',
        axisPointer: { type: 'line', lineStyle: { color: CHART.textSecondary } },
        valueFormatter: (value: number) => formatTry(value),
      },
      xAxis: { ...baseAxisStyle, type: 'time', splitLine: { show: false } },
      yAxis: { ...baseAxisStyle, type: 'value', axisLabel: { ...baseAxisStyle.axisLabel, formatter: formatTryCompact } },
      series: [
        {
          name: 'Bakiye',
          type: 'line',
          step: 'end',
          symbol: 'none',
          lineStyle: { width: 2, color: CHART.series1 },
          areaStyle: { color: CHART.series1, opacity: 0.1 },
          data: points.map((point) => [point.date, point.balance]),
          // Zero is the line that matters: below it the account is overdrawn.
          markLine: {
            symbol: 'none',
            silent: true,
            lineStyle: { color: CHART.critical, width: 1, type: 'solid' },
            label: { formatter: '0 ₺', color: CHART.textSecondary, position: 'insideStartTop' },
            data: [{ yAxis: 0 }],
          },
        },
      ],
    };
  });

  protected readonly monthlyChart = computed<EChartsCoreOption | null>(() => {
    const months = this.projection()?.months;
    if (!months) return null;

    const bar = (name: string, color: string, values: number[]) => ({
      name,
      type: 'bar',
      barMaxWidth: 24,
      barGap: '10%',
      itemStyle: { color, borderRadius: [4, 4, 0, 0] },
      data: values,
    });

    return {
      textStyle: { fontFamily: CHART.font },
      legend: { top: 0, left: 0, icon: 'roundRect', itemWidth: 12, itemHeight: 12, textStyle: { color: CHART.textSecondary } },
      grid: { left: 8, right: 8, top: 36, bottom: 8, containLabel: true },
      tooltip: { ...baseTooltip, trigger: 'axis', axisPointer: { type: 'shadow' }, valueFormatter: (v: number) => formatTry(v) },
      xAxis: { ...baseAxisStyle, type: 'category', data: months.map((m) => this.monthLabel(m)), splitLine: { show: false } },
      yAxis: { ...baseAxisStyle, type: 'value', axisLabel: { ...baseAxisStyle.axisLabel, formatter: formatTryCompact } },
      series: [
        bar('Gelir', CHART.series1, months.map((m) => m.income)),
        bar('Çıkış', CHART.series2, months.map((m) => m.outflows)),
      ],
    };
  });

  constructor() {
    this.load(this.months());
  }

  protected selectHorizon(months: number): void {
    this.months.set(months);
    this.load(months);
  }

  protected monthLabel(month: ProjectionMonth): string {
    return monthName.format(new Date(month.year, month.month - 1, 1));
  }

  protected balanceSummary(): string {
    const lowest = this.lowest();
    if (!lowest) return 'Bakiye grafiği';
    const when = lowest.lowestBalanceDate ? `, ${dayName.format(new Date(lowest.lowestBalanceDate + 'T00:00'))}` : '';
    return `Gün gün tahmini bakiye. En düşük bakiye ${formatTry(lowest.lowestBalance)}${when}.`;
  }

  protected toggleStatement(key: string): void {
    this.openStatement.update((open) => (open === key ? null : key));
  }

  private load(months: number): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.projection(months).subscribe({
      next: (projection) => {
        this.projection.set(projection);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(problemMessage(err, 'Projeksiyon hesaplanamadı.'));
        this.loading.set(false);
      },
    });
  }
}

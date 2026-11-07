import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { EChartsCoreOption } from 'echarts/core';

import { problemMessage } from '../../core/http/problem-details';
import { todayIsoDate } from '../../core/util/dates';
import { CreditCardsApi, PlanningApi } from '../../data-access/api/finance.api';
import { CreditCard, ScenarioComparison, ScenarioMetrics } from '../../data-access/models/finance.models';
import { ChartComponent } from '../../shared/chart/chart.component';
import { CHART, baseAxisStyle, baseTooltip, formatTry, formatTryCompact } from '../../shared/chart/chart-theme';

const MAX_OPTIONS = 6;

// Validated categorical order; three of these sit below 3:1 on white, so the comparison table and the
// legend's text labels carry identity alongside colour.
const OPTION_COLOURS = ['#2a78d6', '#eb6834', '#1baf7a', '#eda100', '#e87ba4', '#008300'];
const BASELINE_COLOUR = '#94a3b8';

const monthName = new Intl.DateTimeFormat('tr-TR', { month: 'short', year: 'numeric' });

export interface ComparisonRow {
  label: string;
  colour: string;
  monthlyInstallment: number | null;
  totalAmount: number | null;
  extraCost: number | null;
  metrics: ScenarioMetrics;
}

@Component({
  selector: 'app-scenarios-page',
  imports: [ReactiveFormsModule, ChartComponent, CurrencyPipe, DatePipe, NgClass],
  templateUrl: './scenarios-page.component.html',
})
export class ScenariosPageComponent {
  private readonly planning = inject(PlanningApi);
  private readonly cardsApi = inject(CreditCardsApi);
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly maxOptions = MAX_OPTIONS;
  protected readonly cards = signal<CreditCard[]>([]);
  protected readonly comparison = signal<ScenarioComparison | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.group({
    creditCardId: [0, [Validators.required, Validators.min(1)]],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    purchaseDate: [todayIsoDate(), Validators.required],
    options: this.fb.array([this.option(3), this.option(6)]),
  });

  protected get options(): FormArray {
    return this.form.controls.options;
  }

  /** Baseline first, then one row per option in the order entered; deliberately not sorted by any "best". */
  protected readonly rows = computed<ComparisonRow[]>(() => {
    const result = this.comparison();
    if (!result) return [];

    const cheapest = Math.min(...result.options.map((o) => o.totalAmount));
    return [
      { label: 'Almasam', colour: BASELINE_COLOUR, monthlyInstallment: null, totalAmount: null, extraCost: null, metrics: result.baseline },
      ...result.options.map((o, i) => ({
        label: `${o.installmentCount} taksit`,
        colour: OPTION_COLOURS[i],
        monthlyInstallment: o.installments[o.installments.length - 1]?.amount ?? null,
        totalAmount: o.totalAmount,
        extraCost: o.totalAmount - cheapest,
        metrics: o.metrics,
      })),
    ];
  });

  protected readonly chart = computed<EChartsCoreOption | null>(() => {
    const rows = this.rows();
    if (rows.length === 0) return null;

    const months = rows[0].metrics.months.map((m) => monthName.format(new Date(m.year, m.month - 1, 1)));
    // Up to four series are also labelled at their line end, so identity never depends on colour.
    const labelEnds = rows.length <= 4;

    return {
      textStyle: { fontFamily: CHART.font },
      legend: { top: 0, left: 0, icon: 'roundRect', itemWidth: 12, itemHeight: 12, textStyle: { color: CHART.textSecondary } },
      grid: { left: 8, right: labelEnds ? 72 : 16, top: 40, bottom: 8, containLabel: true },
      tooltip: { ...baseTooltip, trigger: 'axis', valueFormatter: (v: number) => formatTry(v) },
      xAxis: { ...baseAxisStyle, type: 'category', data: months, boundaryGap: false, splitLine: { show: false } },
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

  constructor() {
    this.cardsApi.list().subscribe({
      next: (cards) => {
        const usable = cards.filter((c) => c.currencyCode === 'TRY');
        this.cards.set(usable);
        if (usable.length > 0) this.form.controls.creditCardId.setValue(usable[0].id);
      },
      error: (err) => this.error.set(problemMessage(err, 'Kartlar yüklenemedi.')),
    });
  }

  protected addOption(): void {
    if (this.options.length >= MAX_OPTIONS) return;
    const lastAmount = this.options.at(this.options.length - 1)?.value.totalAmount ?? 0;
    this.options.push(this.option(9, lastAmount));
  }

  protected removeOption(index: number): void {
    if (this.options.length > 1) this.options.removeAt(index);
  }

  /** Keeps the price in step across options until the user enters a different one for a longer plan. */
  protected copyPriceToAll(): void {
    const price = this.options.at(0).value.totalAmount;
    for (const control of this.options.controls) control.patchValue({ totalAmount: price });
  }

  protected compare(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.busy.set(true);
    this.error.set(null);
    this.planning.compareInstallments({ ...this.form.getRawValue(), months: 3 }).subscribe({
      next: (result) => {
        this.comparison.set(result);
        this.busy.set(false);
      },
      error: (err) => {
        this.error.set(problemMessage(err, 'Karşılaştırma yapılamadı.'));
        this.busy.set(false);
      },
    });
  }

  private option(installmentCount: number, totalAmount = 0) {
    return this.fb.group({
      installmentCount: [installmentCount, [Validators.required, Validators.min(1), Validators.max(36)]],
      totalAmount: [totalAmount, [Validators.required, Validators.min(0.01)]],
    });
  }
}

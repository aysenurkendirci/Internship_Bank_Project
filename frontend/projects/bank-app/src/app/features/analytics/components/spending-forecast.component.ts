import { CurrencyPipe, PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { formatMonth } from '../../../core/util/dates';
import { SpendingForecast } from '../../../data-access/models/finance.models';

const METHOD_LABELS: Record<string, string> = {
  Naive: 'Geçen ay',
  MovingAverage: 'Son 3 ayın ortalaması',
  LinearTrend: 'Son 6 ayın eğilimi',
};

/**
 * Expected everyday spending as a range rather than a single number: the evaluation showed simple
 * methods are reliable for ranges and only moderate for point estimates.
 */
@Component({
  selector: 'app-spending-forecast',
  imports: [CurrencyPipe, PercentPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="grid gap-4 sm:grid-cols-3 mb-6">
      @for (month of months(); track month.label) {
        <li class="rounded-lg bg-canvas p-4">
          <p class="field-label">{{ month.label }}</p>
          <p class="text-xl font-semibold text-ink mt-1">
            {{ month.lower | currency: 'TRY' : 'symbol-narrow' : '1.0-0' }} – {{ month.upper | currency: 'TRY' : 'symbol-narrow' : '1.0-0' }}
          </p>
          <p class="text-sm font-semibold text-ink-soft">beklenen {{ month.expected | currency: 'TRY' : 'symbol-narrow' : '1.0-0' }}</p>
        </li>
      }
    </ul>

    <p class="text-sm font-semibold text-ink-soft mb-3">
      Market, restoran, ulaşım gibi günlük harcamalar; düzenli ödemeler ve tek seferlik büyük alışverişler hariç.
      Aralık %95 olasılıkla. Son {{ forecast().basedOnMonths }} aya göre en isabetli yöntem: <strong>{{ methodLabel(forecast().method) }}</strong>.
    </p>

    <details class="text-sm">
      <summary class="btn-link cursor-pointer">Yöntemler geçmişte ne kadar isabetliydi?</summary>
      <table class="w-full mt-3">
        <thead class="text-left text-ink-faint text-xs">
          <tr><th class="py-2">Yöntem</th><th class="text-right">Ortalama sapma</th><th class="text-right">Aralığın tutturma oranı</th></tr>
        </thead>
        <tbody>
          @for (backtest of forecast().backtests; track backtest.method) {
            <tr class="border-t border-line font-semibold text-ink-soft">
              <td class="py-2">{{ methodLabel(backtest.method) }}</td>
              <td class="text-right">{{ backtest.meanAbsolutePercentageError | percent: '1.0-1' }}</td>
              <td class="text-right">{{ backtest.intervalCoverage | percent: '1.0-0' }}</td>
            </tr>
          }
        </tbody>
      </table>
    </details>
  `,
})
export class SpendingForecastComponent {
  readonly forecast = input.required<SpendingForecast>();

  protected readonly months = computed(() =>
    this.forecast().months.map((m) => ({ ...m, label: formatMonth(m.month, 'long') })),
  );

  protected methodLabel(method: string): string {
    return METHOD_LABELS[method] ?? method;
  }
}

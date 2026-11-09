import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { ComparisonRow } from '../comparison-rows';

/** One row per option plus the no-purchase baseline; also the accessible view of the scenario chart. */
@Component({
  selector: 'app-scenario-comparison-table',
  imports: [CurrencyPipe, DatePipe, NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block overflow-x-auto' },
  template: `
    <table class="w-full text-sm min-w-[820px]">
      <thead class="text-left text-ink-faint text-xs">
        <tr>
          <th class="py-2">Seçenek</th>
          <th class="text-right">Aylık taksit</th>
          <th class="text-right">Toplam</th>
          <th class="text-right">Fiyat farkı</th>
          <th class="text-right">En yüksek aylık çıkış</th>
          <th class="text-right">En düşük bakiye</th>
          <th class="text-right">Eksiye düşülen ay</th>
          <th class="text-right">Dönem sonu bakiye</th>
        </tr>
      </thead>
      <tbody>
        @for (row of rows(); track row.label) {
          <tr class="border-t border-line font-semibold text-ink-soft">
            <td class="py-3">
              <span class="inline-flex items-center gap-2">
                <span class="w-3 h-3 rounded" [style.background]="row.colour" aria-hidden="true"></span>
                {{ row.label }}
              </span>
            </td>
            <td class="text-right">{{ row.monthlyInstallment === null ? '—' : (row.monthlyInstallment | currency) }}</td>
            <td class="text-right">{{ row.totalAmount === null ? '—' : (row.totalAmount | currency) }}</td>
            <td class="text-right">{{ row.extraCost === null ? '—' : (row.extraCost | currency) }}</td>
            <td class="text-right">{{ row.metrics.peakMonthlyOutflow | currency }}</td>
            <td class="text-right" [ngClass]="row.metrics.lowestBalance < 0 ? 'text-danger' : ''">
              {{ row.metrics.lowestBalance | currency }}
              @if (row.metrics.lowestBalanceDate) {
                <span class="block text-xs text-ink-faint">{{ row.metrics.lowestBalanceDate | date: 'd MMM y' }}</span>
              }
            </td>
            <td class="text-right">
              @if (row.metrics.monthsBelowZero > 0) {
                <span class="inline-flex items-center gap-1 text-danger">
                  <span class="material-symbols-outlined text-base" aria-hidden="true">warning</span>
                  {{ row.metrics.monthsBelowZero }} ay
                </span>
              } @else {
                <span class="text-ink-faint">Yok</span>
              }
            </td>
            <td class="text-right">{{ row.metrics.closingBalance | currency }}</td>
          </tr>
        }
      </tbody>
    </table>
  `,
})
export class ScenarioComparisonTableComponent {
  readonly rows = input.required<readonly ComparisonRow[]>();
}

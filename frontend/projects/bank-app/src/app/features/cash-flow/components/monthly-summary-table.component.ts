import { CurrencyPipe, NgClass, PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { formatMonth } from '../../../core/util/dates';
import { ProjectionMonth } from '../../../data-access/models/finance.models';

/** Month-by-month figures; also the accessible view of the projection charts. */
@Component({
  selector: 'app-monthly-summary-table',
  imports: [CurrencyPipe, NgClass, PercentPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block overflow-x-auto' },
  template: `
    <table class="w-full text-sm min-w-[720px]">
      <thead class="text-left text-ink-faint text-xs">
        <tr>
          <th class="py-2">Ay</th>
          <th class="text-right">Gelir</th>
          <th class="text-right">Çıkış</th>
          <th class="text-right">Kart ödemeleri</th>
          <th class="text-right">Net</th>
          <th class="text-right">Ay sonu</th>
          <th class="text-right">Ay içi en düşük</th>
          <th class="text-right">Yük oranı</th>
          <th class="pl-4">Durum</th>
        </tr>
      </thead>
      <tbody>
        @for (m of months(); track m.year * 100 + m.month) {
          <tr class="border-t border-line font-medium text-ink-soft">
            <td class="py-3">{{ label(m) }}</td>
            <td class="text-right">{{ m.income | currency }}</td>
            <td class="text-right">{{ m.outflows | currency }}</td>
            <td class="text-right">{{ m.cardPayments | currency }}</td>
            <td class="text-right" [ngClass]="m.net < 0 ? 'text-danger' : ''">{{ m.net | currency }}</td>
            <td class="text-right">{{ m.closingBalance | currency }}</td>
            <td class="text-right" [ngClass]="m.lowestBalance < 0 ? 'text-danger' : ''">{{ m.lowestBalance | currency }}</td>
            <td class="text-right">{{ m.burdenRatio === null ? '—' : (m.burdenRatio | percent: '1.0-0') }}</td>
            <td class="pl-4">
              @if (m.balanceGoesNegative) {
                <span class="inline-flex items-center gap-1 text-danger">
                  <span class="material-symbols-outlined text-base" aria-hidden="true">warning</span>
                  Eksiye düşüyor
                </span>
              } @else {
                <span class="text-ink-faint">Sorun yok</span>
              }
            </td>
          </tr>
        }
      </tbody>
    </table>
    <p class="text-xs text-ink-faint mt-3">
      Yük oranı: ayın toplam çıkışı / ayın geliri. Kart ödemeleri ekstrenin tamamının ödendiği varsayımıyla hesaplanır.
    </p>
  `,
})
export class MonthlySummaryTableComponent {
  readonly months = input.required<readonly ProjectionMonth[]>();

  protected label(month: ProjectionMonth): string {
    return formatMonth(month);
  }
}

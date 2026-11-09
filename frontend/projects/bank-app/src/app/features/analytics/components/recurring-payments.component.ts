import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { RecurringPayment } from '../../../data-access/models/finance.models';
import { EmptyStateComponent } from '../../../shared/ui/empty-state.component';

/**
 * Payments found to repeat monthly. Untracked ones can be added as recurring items in one click,
 * which is what brings forgotten subscriptions into the cash-flow projection.
 */
@Component({
  selector: 'app-recurring-payments',
  imports: [EmptyStateComponent, CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (payments().length === 0) {
      <app-empty-state>En az üç aylık geçmişte düzenli tekrar eden bir ödeme bulunamadı.</app-empty-state>
    }

    <ul class="divide-y divide-line">
      @for (payment of payments(); track payment.merchant + payment.direction) {
        <li class="py-3 flex flex-wrap items-center justify-between gap-3">
          <div>
            <p class="font-semibold text-ink">{{ payment.merchant }}</p>
            <p class="text-sm text-ink-soft">
              {{ payment.direction === 'Inbound' ? 'Gelir' : payment.pattern === 'Fixed' ? 'Sabit tutarlı' : 'Tutarı değişen' }}
              · ayın {{ payment.typicalDay }}. günü civarı · {{ payment.monthsObserved }} ayda görüldü
              · sıradaki: {{ payment.nextExpected | date: 'd MMM y' }}
            </p>
          </div>
          <div class="flex items-center gap-4">
            <span class="font-semibold text-ink">{{ payment.typicalAmount | currency }}</span>
            @if (payment.isTracked) {
              <span class="badge bg-positive-tint text-positive">Takipte</span>
            } @else {
              <button type="button" class="btn-link" [disabled]="busyMerchant() !== null" (click)="track.emit(payment)">
                {{ busyMerchant() === payment.merchant ? 'Ekleniyor...' : 'Takibe al' }}
              </button>
            }
          </div>
        </li>
      }
    </ul>
  `,
})
export class RecurringPaymentsComponent {
  readonly payments = input.required<readonly RecurringPayment[]>();
  /** The merchant currently being added, so its button can show progress. */
  readonly busyMerchant = input<string | null>(null);
  readonly track = output<RecurringPayment>();
}

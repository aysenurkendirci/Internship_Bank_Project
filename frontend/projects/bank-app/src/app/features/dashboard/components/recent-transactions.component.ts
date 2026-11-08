import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { TransactionItem } from '../../../data-access/models/dashboard.models';

@Component({
  selector: 'app-recent-transactions',
  imports: [CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <h2 class="section-title mb-3">Son işlemler</h2>
    <div class="bg-white rounded-xl border border-line">
      @if (transactions().length === 0) {
        <p class="p-5 text-sm text-ink-soft">Henüz işlem yok.</p>
      }
      <ul class="divide-y divide-line">
        @for (tx of transactions(); track tx.txId) {
          <li class="flex items-center justify-between gap-4 px-4 py-3">
            <div class="min-w-0">
              <p class="text-sm font-medium text-ink truncate">{{ tx.title }}</p>
              <p class="text-xs text-ink-faint mt-0.5">{{ tx.category }} · {{ tx.createdAt | date: 'd MMM y, HH:mm' }}</p>
            </div>
            <p class="text-sm font-medium tabular-nums shrink-0" [class]="tx.direction === 'Inbound' ? 'text-positive' : 'text-ink'">
              {{ tx.direction === 'Inbound' ? '+' : '−' }}{{ tx.amount | currency }}
            </p>
          </li>
        }
      </ul>
    </div>
  `,
})
export class RecentTransactionsComponent {
  readonly transactions = input.required<readonly TransactionItem[]>();
}

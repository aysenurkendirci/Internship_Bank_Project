import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';

import { formatIban } from '../../../core/util/iban';
import { AccountItem } from '../../../data-access/models/dashboard.models';
import { ACCOUNT_STATUS_LABELS, ACCOUNT_TYPE_DISPLAY } from '../account-display';

/** The customer's accounts on the home screen. Clicking an IBAN copies it. */
@Component({
  selector: 'app-account-list',
  imports: [CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <h2 class="section-title mb-3">Hesaplarım</h2>
    <div class="bg-surface rounded-xl border border-line">
      @if (accounts().length === 0) {
        <p class="p-5 text-sm text-ink-soft">Henüz hesabınız yok.</p>
      }
      <ul class="divide-y divide-line">
        @for (account of accounts(); track account.accountId) {
          <li class="px-4 py-3">
            <div class="flex items-baseline justify-between gap-3">
              <p class="text-sm font-medium text-ink">
                {{ typeDisplay[account.accountType].label }}
                @if (account.status !== 'Active') {
                  <span class="badge bg-canvas text-ink-soft ml-1">{{ statusLabels[account.status] }}</span>
                }
              </p>
              <p class="text-sm font-semibold tabular-nums">{{ account.balance | currency: account.currencyCode : 'symbol-narrow' }}</p>
            </div>
            <button
              type="button"
              (click)="copy(account)"
              class="mt-1 text-xs text-ink-faint tabular-nums hover:text-ink text-left"
              title="IBAN'ı kopyala"
            >
              {{ copiedId() === account.accountId ? 'Kopyalandı' : printed(account.iban) }}
            </button>
          </li>
        }
      </ul>
    </div>
  `,
})
export class AccountListComponent {
  readonly accounts = input.required<readonly AccountItem[]>();

  protected readonly typeDisplay = ACCOUNT_TYPE_DISPLAY;
  protected readonly statusLabels = ACCOUNT_STATUS_LABELS;
  protected readonly copiedId = signal<number | null>(null);
  protected readonly printed = formatIban;

  protected copy(account: AccountItem): void {
    navigator.clipboard.writeText(account.iban).then(() => {
      this.copiedId.set(account.accountId);
      setTimeout(() => this.copiedId.set(null), 1500);
    });
  }
}

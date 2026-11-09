import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AccountItem } from '../../../data-access/models/dashboard.models';
import { ACCOUNT_STATUS_LABELS, ACCOUNT_TYPE_DISPLAY } from '../../../shared/finance/account-display';
import { IbanCopyComponent } from '../../../shared/finance/iban-copy.component';
import { MoneyComponent } from '../../../shared/finance/money.component';

/** The customer's accounts on the home screen. Each one leads to its statement. */
@Component({
  selector: 'app-account-list',
  imports: [RouterLink, IbanCopyComponent, MoneyComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <div class="flex items-center justify-between mb-3">
      <h2 class="section-title">Hesaplarım</h2>
      <a routerLink="/dashboard/accounts" class="btn-link">Tümü</a>
    </div>
    <div class="bg-surface rounded-xl border border-line">
      @if (accounts().length === 0) {
        <p class="p-5 text-sm text-ink-soft">Henüz hesabınız yok.</p>
      }
      <ul class="divide-y divide-line">
        @for (account of accounts(); track account.accountId) {
          <li class="px-4 py-3">
            <div class="flex items-baseline justify-between gap-3">
              <a class="text-sm font-medium text-ink hover:text-accent" [routerLink]="['/dashboard/accounts', account.accountId]">
                {{ typeDisplay[account.accountType].label }}
                @if (account.status !== 'Active') {
                  <span class="badge bg-canvas text-ink-soft ml-1">{{ statusLabels[account.status] }}</span>
                }
              </a>
              <p class="text-sm font-semibold tabular-nums"><app-money [amount]="account.balance" [currency]="account.currencyCode" /></p>
            </div>
            <app-iban-copy class="block mt-1" [iban]="account.iban" />
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
}

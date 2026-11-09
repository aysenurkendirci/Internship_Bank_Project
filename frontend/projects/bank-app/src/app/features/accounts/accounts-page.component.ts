import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { BasePage } from '../../core/page/base-page';
import { ToastService } from '../../core/ui/toast.service';
import { AccountsApi } from '../../data-access/api/banking.api';
import { Account, AccountCurrency } from '../../data-access/models/banking.models';
import { ACCOUNT_STATUS_LABELS, ACCOUNT_TYPE_DISPLAY } from '../../shared/finance/account-display';
import { IbanCopyComponent } from '../../shared/finance/iban-copy.component';
import { MoneyComponent } from '../../shared/finance/money.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { SkeletonComponent } from '../../shared/ui/skeleton.component';

/** The customer's accounts, with the way into each statement and into opening a new account. */
@Component({
  imports: [FormsModule, RouterLink, IbanCopyComponent, MoneyComponent, EmptyStateComponent, NoticeComponent, PageHeaderComponent, SkeletonComponent],
  template: `
    <app-page-header title="Hesaplarım" subtitle="Bakiyeleriniz ve hesap hareketleriniz.">
      <button type="button" class="btn-secondary" (click)="opening.set(!opening())">Hesap aç</button>
      <a routerLink="/dashboard/transfer" class="btn-primary">Para gönder</a>
    </app-page-header>

    @if (error(); as message) {
      <app-notice class="mb-4" [message]="message" />
    }

    @if (opening()) {
      <form class="panel mb-6 flex flex-wrap items-end gap-3" (ngSubmit)="open()">
        <label class="field">
          <span class="field-label">Para birimi</span>
          <select class="input" name="currency" [(ngModel)]="currency">
            @for (option of currencies; track option.code) {
              <option [ngValue]="option.code">{{ option.label }}</option>
            }
          </select>
        </label>
        <button type="submit" class="btn-primary" [disabled]="busy()">{{ busy() ? 'Açılıyor...' : 'Hesabı aç' }}</button>
        <button type="button" class="btn-secondary" (click)="opening.set(false)">Vazgeç</button>
      </form>
    }

    @if (loading() && accounts().length === 0) {
      <app-skeleton />
    } @else if (accounts().length === 0 && !error()) {
      <app-empty-state>Henüz hesabınız yok. "Hesap aç" ile ilk hesabınızı oluşturabilirsiniz.</app-empty-state>
    } @else {
      <ul class="bg-surface rounded-xl border border-line divide-y divide-line">
        @for (account of accounts(); track account.id) {
          <li class="px-4 py-4 flex flex-wrap items-center justify-between gap-x-6 gap-y-2">
            <div class="min-w-0">
              <p class="text-sm font-medium text-ink">
                {{ typeDisplay[account.accountType].label }} · {{ account.currencyCode }}
                @if (account.status !== 'Active') {
                  <span class="badge bg-canvas text-ink-soft ml-1">{{ statusLabels[account.status] }}</span>
                }
              </p>
              <app-iban-copy class="block mt-1" [iban]="account.iban" />
            </div>
            <div class="flex items-center gap-5">
              <p class="text-base font-semibold tabular-nums"><app-money [amount]="account.balance" [currency]="account.currencyCode" /></p>
              <a class="btn-link" [routerLink]="['/dashboard/accounts', account.id]">Hareketler</a>
              @if (account.status === 'Active') {
                <a class="btn-link" routerLink="/dashboard/transfer" [queryParams]="{ from: account.id }">Gönder</a>
              }
            </div>
          </li>
        }
      </ul>
    }
  `,
})
export class AccountsPageComponent extends BasePage {
  private readonly api = inject(AccountsApi);
  private readonly toast = inject(ToastService);

  protected readonly typeDisplay = ACCOUNT_TYPE_DISPLAY;
  protected readonly statusLabels = ACCOUNT_STATUS_LABELS;
  protected readonly currencies: readonly { code: AccountCurrency; label: string }[] = [
    { code: 'TRY', label: 'Türk Lirası' },
    { code: 'USD', label: 'ABD Doları' },
    { code: 'EUR', label: 'Euro' },
  ];

  protected readonly accounts = signal<readonly Account[]>([]);
  protected readonly opening = signal(false);
  protected currency: AccountCurrency = 'TRY';

  constructor() {
    super();
    this.fetch();
  }

  protected open(): void {
    this.act(this.api.open(this.currency), {
      fallback: 'Hesap açılamadı.',
      onDone: () => {
        this.opening.set(false);
        this.toast.show('Hesabınız açıldı.');
        this.fetch();
      },
    });
  }

  private fetch(): void {
    this.load(this.api.list(), (accounts) => this.accounts.set(accounts), 'Hesaplar yüklenemedi.');
  }
}

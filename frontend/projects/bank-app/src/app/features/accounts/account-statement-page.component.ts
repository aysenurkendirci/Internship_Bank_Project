import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { BasePage } from '../../core/page/base-page';
import { formatIban } from '../../core/util/iban';
import { AccountsApi } from '../../data-access/api/banking.api';
import { Account, StatementEntry } from '../../data-access/models/banking.models';
import { ACCOUNT_TYPE_DISPLAY } from '../../shared/finance/account-display';
import { IbanCopyComponent } from '../../shared/finance/iban-copy.component';
import { MoneyComponent } from '../../shared/finance/money.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { PagerComponent } from '../../shared/ui/pager.component';
import { SkeletonComponent } from '../../shared/ui/skeleton.component';

const PAGE_SIZE = 20;

/** One account's movements, read from the ledger, newest first. */
@Component({
  imports: [CurrencyPipe, DatePipe, RouterLink, IbanCopyComponent, MoneyComponent, EmptyStateComponent, NoticeComponent, PagerComponent, SkeletonComponent],
  template: `
    <a routerLink="/dashboard/accounts" class="btn-link">Hesaplarım</a>

    @if (account(); as account) {
      <header class="mt-3 mb-6 panel flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="section-title">{{ typeDisplay[account.accountType].label }} · {{ account.currencyCode }}</h1>
          <app-iban-copy class="block mt-1" [iban]="account.iban" />
        </div>
        <div class="text-right">
          <p class="text-sm text-ink-soft">Bakiye</p>
          <p class="text-2xl font-semibold tabular-nums"><app-money [amount]="account.balance" [currency]="account.currencyCode" /></p>
        </div>
      </header>
    }

    @if (error(); as message) {
      <app-notice class="my-4" [message]="message" />
    }

    @if (loading() && entries().length === 0) {
      <app-skeleton class="mt-4" [rows]="5" />
    } @else if (entries().length === 0 && !error()) {
      <app-empty-state>Bu hesapta henüz hareket yok.</app-empty-state>
    } @else {
      <ul class="bg-surface rounded-xl border border-line divide-y divide-line">
        @for (entry of entries(); track entry.entryId) {
          <li class="flex items-center justify-between gap-4 px-4 py-3">
            <div class="min-w-0">
              <p class="text-sm font-medium text-ink truncate">{{ entry.description }}</p>
              <p class="text-xs text-ink-faint mt-0.5 truncate">
                {{ entry.createdAt | date: 'd MMM y, HH:mm' }} · {{ printed(entry.counterpartyIban) }}
              </p>
            </div>
            <div class="text-right shrink-0">
              <p class="text-sm font-medium tabular-nums" [class]="entry.side === 'Credit' ? 'text-positive' : 'text-ink'">
                {{ entry.side === 'Credit' ? '+' : '−' }}{{ entry.amount | currency: entry.currencyCode : 'symbol-narrow' }}
              </p>
              <p class="text-xs text-ink-faint tabular-nums mt-0.5">{{ entry.balanceAfter | currency: entry.currencyCode : 'symbol-narrow' }}</p>
            </div>
          </li>
        }
      </ul>
      <app-pager [page]="page()" [pageSize]="pageSize" [totalCount]="totalCount()" (pageChange)="goTo($event)" />
    }
  `,
})
export class AccountStatementPageComponent extends BasePage implements OnInit {
  /** From the route: /dashboard/accounts/:id */
  readonly id = input.required<string>();

  private readonly api = inject(AccountsApi);
  private readonly accountId = computed(() => Number(this.id()));

  protected readonly typeDisplay = ACCOUNT_TYPE_DISPLAY;
  protected readonly printed = formatIban;
  protected readonly pageSize = PAGE_SIZE;

  protected readonly account = signal<Account | null>(null);
  protected readonly entries = signal<readonly StatementEntry[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);

  ngOnInit(): void {
    // The API has no single-account read; the list is short and also proves the account is the customer's.
    this.api.list().subscribe({ next: (accounts) => this.account.set(accounts.find((a) => a.id === this.accountId()) ?? null) });
    this.fetch();
  }

  protected goTo(page: number): void {
    this.page.set(page);
    this.fetch();
  }

  private fetch(): void {
    this.load(
      this.api.statement(this.accountId(), this.page(), PAGE_SIZE),
      (result) => {
        this.entries.set(result.items);
        this.totalCount.set(result.totalCount);
      },
      'Hesap hareketleri yüklenemedi.',
    );
  }
}

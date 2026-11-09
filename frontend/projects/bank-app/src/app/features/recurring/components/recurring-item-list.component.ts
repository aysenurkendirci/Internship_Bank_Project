import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { CreditCard, RecurringCashFlow } from '../../../data-access/models/finance.models';
import { KIND_LABELS, describeSchedule } from '../recurring-labels';

/** Recurring items with their type, schedule and amount. */
@Component({
  selector: 'app-recurring-item-list',
  imports: [CurrencyPipe, DatePipe, NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="space-y-3">
      @for (item of items(); track item.id) {
        <li class="panel flex flex-wrap items-center justify-between gap-4">
          <div class="flex items-center gap-4">
            <span
              class="badge"
              [ngClass]="{
                'bg-positive-tint text-positive': item.kind === 'Income',
                'bg-canvas text-ink-soft': item.kind === 'Expense',
                'bg-brand-tint text-brand': item.kind === 'Subscription'
              }"
            >{{ kindLabels[item.kind] }}</span>
            <div>
              <p class="font-semibold text-ink">{{ item.name }}</p>
              <p class="text-sm font-semibold text-ink-soft">
                {{ item.category }} · {{ schedule(item) }} · Başlangıç {{ item.startDate | date: 'd MMM y' }}
                @if (item.endDate) { · Bitiş {{ item.endDate | date: 'd MMM y' }} }
                @if (cardName(item.creditCardId); as card) { · {{ card }} kartından }
              </p>
            </div>
          </div>
          <div class="flex items-center gap-4">
            <p class="font-semibold" [ngClass]="item.kind === 'Income' ? 'text-positive' : 'text-ink'">
              {{ item.amount | currency: item.currencyCode : 'symbol-narrow' }}
            </p>
            <button type="button" class="btn-link" (click)="edit.emit(item)">Düzenle</button>
            <button type="button" class="btn-link text-danger" (click)="remove.emit(item)" [disabled]="busy()">Sil</button>
          </div>
        </li>
      }
    </ul>
  `,
})
export class RecurringItemListComponent {
  readonly items = input.required<readonly RecurringCashFlow[]>();
  readonly cards = input.required<readonly CreditCard[]>();
  readonly busy = input(false);
  readonly edit = output<RecurringCashFlow>();
  readonly remove = output<RecurringCashFlow>();

  protected readonly kindLabels = KIND_LABELS;

  protected schedule(item: RecurringCashFlow): string {
    return describeSchedule(item.frequency, item.interval);
  }

  protected cardName(id: number | null): string | null {
    return id === null ? null : (this.cards().find((c) => c.id === id)?.name ?? null);
  }
}

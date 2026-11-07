import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { CreditCard, RecurringCashFlow } from '../../../data-access/models/finance.models';
import { KIND_LABELS, describeSchedule } from '../recurring-labels';

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
                'bg-emerald-50 text-emerald-700': item.kind === 'Income',
                'bg-slate-100 text-slate-700': item.kind === 'Expense',
                'bg-violet-50 text-violet-700': item.kind === 'Subscription'
              }"
            >{{ kindLabels[item.kind] }}</span>
            <div>
              <p class="font-black text-slate-900">{{ item.name }}</p>
              <p class="text-sm font-semibold text-slate-500">
                {{ item.category }} · {{ schedule(item) }} · Başlangıç {{ item.startDate | date: 'd MMM y' }}
                @if (item.endDate) { · Bitiş {{ item.endDate | date: 'd MMM y' }} }
                @if (cardName(item.creditCardId); as card) { · {{ card }} kartından }
              </p>
            </div>
          </div>
          <div class="flex items-center gap-4">
            <p class="font-black" [ngClass]="item.kind === 'Income' ? 'text-emerald-600' : 'text-slate-900'">
              {{ item.amount | currency: item.currencyCode : 'symbol-narrow' }}
            </p>
            <button type="button" class="btn-link" (click)="edit.emit(item)">Düzenle</button>
            <button type="button" class="btn-link text-red-600" (click)="remove.emit(item)" [disabled]="busy()">Sil</button>
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

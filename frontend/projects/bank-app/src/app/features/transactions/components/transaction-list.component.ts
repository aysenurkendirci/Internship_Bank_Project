import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { HistoryTransaction, IsoDate } from '../../../data-access/models/finance.models';

export interface DateRange {
  from?: IsoDate;
  to?: IsoDate;
}

/** A page of transactions with a date filter and pager; it asks the container for data, never the API. */
@Component({
  selector: 'app-transaction-list',
  imports: [FormsModule, CurrencyPipe, DatePipe, NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'panel block' },
  template: `
    <form class="flex flex-wrap items-end gap-3 mb-4" (ngSubmit)="filter.emit({ from: from() || undefined, to: to() || undefined })">
      <label class="field">
        <span class="field-label">Başlangıç</span>
        <input class="input" type="date" name="from" [ngModel]="from()" (ngModelChange)="from.set($event)" />
      </label>
      <label class="field">
        <span class="field-label">Bitiş</span>
        <input class="input" type="date" name="to" [ngModel]="to()" (ngModelChange)="to.set($event)" />
      </label>
      <button type="submit" class="btn-secondary">Filtrele</button>
      <span class="ml-auto text-sm font-bold text-slate-400">{{ totalCount() }} işlem</span>
    </form>

    @if (items().length === 0) {
      <p class="text-slate-500 font-semibold">Gösterilecek işlem yok. Bir ekstre yükleyin veya demo verisi oluşturun.</p>
    }

    <ul class="divide-y divide-slate-100">
      @for (t of items(); track t.id) {
        <li class="py-3 flex flex-wrap items-center justify-between gap-2">
          <div class="flex items-center gap-3">
            <span class="text-sm font-bold text-slate-400 w-24">{{ t.date | date: 'd MMM y' }}</span>
            <span class="font-bold text-slate-800">{{ t.description }}</span>
            @if (t.category) {
              <span class="badge bg-slate-100 text-slate-600">{{ t.category }}</span>
            }
          </div>
          <span class="font-black" [ngClass]="t.amount < 0 ? 'text-slate-900' : 'text-emerald-600'">
            {{ t.amount | currency: t.currencyCode : 'symbol-narrow' }}
          </span>
        </li>
      }
    </ul>

    @if (pageCount() > 1) {
      <nav class="flex items-center justify-center gap-4 mt-6" aria-label="Sayfalar">
        <button type="button" class="btn-secondary" (click)="pageChange.emit(page() - 1)" [disabled]="page() === 1">Önceki</button>
        <span class="text-sm font-bold text-slate-500">{{ page() }} / {{ pageCount() }}</span>
        <button type="button" class="btn-secondary" (click)="pageChange.emit(page() + 1)" [disabled]="page() === pageCount()">Sonraki</button>
      </nav>
    }
  `,
})
export class TransactionListComponent {
  readonly items = input.required<readonly HistoryTransaction[]>();
  readonly totalCount = input.required<number>();
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();

  readonly filter = output<DateRange>();
  readonly pageChange = output<number>();

  protected readonly from = signal('');
  protected readonly to = signal('');
  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
}

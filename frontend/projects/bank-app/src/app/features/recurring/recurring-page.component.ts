import { NgClass } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';

import { problemMessage } from '../../core/http/problem-details';
import { CreditCardsApi, RecurringCashFlowsApi } from '../../data-access/api/finance.api';
import { CashFlowKind, CreditCard, RecurringCashFlow, RecurringCashFlowRequest } from '../../data-access/models/finance.models';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { RecurringItemFormComponent } from './components/recurring-item-form.component';
import { RecurringItemListComponent } from './components/recurring-item-list.component';
import { KIND_LABELS } from './recurring-labels';

@Component({
  selector: 'app-recurring-page',
  imports: [NgClass, PageHeaderComponent, NoticeComponent, EmptyStateComponent, RecurringItemFormComponent, RecurringItemListComponent],
  templateUrl: './recurring-page.component.html',
})
export class RecurringPageComponent {
  private readonly api = inject(RecurringCashFlowsApi);
  private readonly cardsApi = inject(CreditCardsApi);

  protected readonly kindLabels = KIND_LABELS;
  protected readonly kinds = Object.keys(KIND_LABELS) as CashFlowKind[];

  protected readonly items = signal<RecurringCashFlow[]>([]);
  protected readonly cards = signal<CreditCard[]>([]);
  protected readonly filter = signal<CashFlowKind | null>(null);
  protected readonly loaded = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editing = signal<RecurringCashFlow | null | undefined>(undefined);

  protected readonly visibleItems = computed(() => {
    const kind = this.filter();
    return kind ? this.items().filter((i) => i.kind === kind) : this.items();
  });

  constructor() {
    this.reload();
  }

  protected save(request: RecurringCashFlowRequest): void {
    const item = this.editing();
    const call = item ? this.api.update(item.id, request, item.version) : this.api.create(request);
    this.run(call, () => this.editing.set(undefined));
  }

  protected remove(item: RecurringCashFlow): void {
    if (confirm(`"${item.name}" silinsin mi?`)) this.run(this.api.delete(item.id));
  }

  private reload(): void {
    forkJoin({ items: this.api.list(), cards: this.cardsApi.list() }).subscribe({
      next: ({ items, cards }) => {
        this.items.set(items);
        this.cards.set(cards);
        this.loaded.set(true);
      },
      error: (err) => this.error.set(problemMessage(err, 'Kayıtlar yüklenemedi.')),
    });
  }

  private run<T>(call: Observable<T>, onSuccess?: () => void): void {
    this.busy.set(true);
    this.error.set(null);
    call.subscribe({
      next: () => {
        this.busy.set(false);
        onSuccess?.();
        this.reload();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(problemMessage(err));
        if (err instanceof HttpErrorResponse && err.status === 409) {
          this.editing.set(undefined);
          this.reload();
        }
      },
    });
  }
}

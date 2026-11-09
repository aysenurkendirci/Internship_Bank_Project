import { NgClass } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';

import { ActOptions, BasePage } from '../../core/page/base-page';
import { CreditCardsApi, RecurringCashFlowsApi } from '../../data-access/api/finance.api';
import { CashFlowKind, CreditCard, RecurringCashFlow, RecurringCashFlowRequest } from '../../data-access/models/finance.models';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { RecurringItemFormComponent } from './components/recurring-item-form.component';
import { RecurringItemListComponent } from './components/recurring-item-list.component';
import { KIND_LABELS } from './recurring-labels';

/** Container: loads items and cards, filters by type and runs every change. */
@Component({
  selector: 'app-recurring-page',
  imports: [NgClass, PageHeaderComponent, NoticeComponent, EmptyStateComponent, RecurringItemFormComponent, RecurringItemListComponent],
  templateUrl: './recurring-page.component.html',
})
export class RecurringPageComponent extends BasePage {
  private readonly api = inject(RecurringCashFlowsApi);
  private readonly cardsApi = inject(CreditCardsApi);

  protected readonly kindLabels = KIND_LABELS;
  protected readonly kinds = Object.keys(KIND_LABELS) as CashFlowKind[];

  protected readonly items = signal<RecurringCashFlow[]>([]);
  protected readonly cards = signal<CreditCard[]>([]);
  protected readonly filter = signal<CashFlowKind | null>(null);
  protected readonly loaded = signal(false);
  protected readonly editing = signal<RecurringCashFlow | null | undefined>(undefined);

  protected readonly visibleItems = computed(() => {
    const kind = this.filter();
    return kind ? this.items().filter((i) => i.kind === kind) : this.items();
  });

  constructor() {
    super();
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
    this.load(
      forkJoin({ items: this.api.list(), cards: this.cardsApi.list() }),
      ({ items, cards }) => {
        this.items.set(items);
        this.cards.set(cards);
        this.loaded.set(true);
      },
      'Kayıtlar yüklenemedi.',
    );
  }

  /** Runs a change and reloads. A 409 means the data moved underneath the user, so the edit is abandoned. */
  private run<T>(call: Observable<T>, onSuccess?: () => void): void {
    this.act(call, {
      onDone: () => {
        onSuccess?.();
        this.reload();
      },
      onConflict: () => {
        this.editing.set(undefined);
        this.reload();
      },
    } satisfies ActOptions<T>);
  }
}

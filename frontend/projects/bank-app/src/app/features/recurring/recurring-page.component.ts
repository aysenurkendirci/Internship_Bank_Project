import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, forkJoin } from 'rxjs';

import { problemMessage } from '../../core/http/problem-details';
import { todayIsoDate } from '../../core/util/dates';
import { CreditCardsApi, RecurringCashFlowsApi } from '../../data-access/api/finance.api';
import {
  CashFlowKind,
  CreditCard,
  CurrencyCode,
  Frequency,
  RecurringCashFlow,
  RecurringCashFlowRequest,
} from '../../data-access/models/finance.models';

export const KIND_LABELS: Record<CashFlowKind, string> = {
  Income: 'Gelir',
  Expense: 'Gider',
  Subscription: 'Abonelik',
};

const FREQUENCY_LABELS: Record<Frequency, string> = {
  Weekly: 'hafta',
  Monthly: 'ay',
  Yearly: 'yıl',
};

@Component({
  selector: 'app-recurring-page',
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, NgClass],
  templateUrl: './recurring-page.component.html',
})
export class RecurringPageComponent {
  private readonly api = inject(RecurringCashFlowsApi);
  private readonly cardsApi = inject(CreditCardsApi);
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly kindLabels = KIND_LABELS;
  protected readonly kinds = Object.keys(KIND_LABELS) as CashFlowKind[];

  protected readonly items = signal<RecurringCashFlow[]>([]);
  protected readonly cards = signal<CreditCard[]>([]);
  protected readonly filter = signal<CashFlowKind | null>(null);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editing = signal<RecurringCashFlow | null | undefined>(undefined);

  protected readonly visibleItems = computed(() => {
    const kind = this.filter();
    return kind ? this.items().filter((i) => i.kind === kind) : this.items();
  });

  protected readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    kind: ['Expense' as CashFlowKind, Validators.required],
    category: ['', [Validators.required, Validators.maxLength(50)]],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    currencyCode: ['TRY' as CurrencyCode, Validators.required],
    frequency: ['Monthly' as Frequency, Validators.required],
    interval: [1, [Validators.required, Validators.min(1), Validators.max(52)]],
    startDate: [todayIsoDate(), Validators.required],
    endDate: [''],
    creditCardId: [0],
  });

  // Income can never be charged to a card; the API rejects it, so the field is not even offered.
  protected readonly selectedKind = toSignal(this.form.controls.kind.valueChanges, { initialValue: this.form.controls.kind.value });
  protected readonly canUseCard = computed(() => this.selectedKind() !== 'Income');

  constructor() {
    this.reload();
  }

  protected describeSchedule(item: RecurringCashFlow): string {
    const unit = FREQUENCY_LABELS[item.frequency];
    return item.interval === 1 ? `Her ${unit}` : `${item.interval} ${unit}da bir`;
  }

  protected cardName(id: number | null): string | null {
    return id === null ? null : (this.cards().find((c) => c.id === id)?.name ?? null);
  }

  protected startAdding(): void {
    this.form.reset();
    this.editing.set(null);
  }

  protected startEditing(item: RecurringCashFlow): void {
    this.form.setValue({
      name: item.name,
      kind: item.kind,
      category: item.category,
      amount: item.amount,
      currencyCode: item.currencyCode,
      frequency: item.frequency,
      interval: item.interval,
      startDate: item.startDate,
      endDate: item.endDate ?? '',
      creditCardId: item.creditCardId ?? 0,
    });
    this.editing.set(item);
  }

  protected save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    const request: RecurringCashFlowRequest = {
      ...value,
      endDate: value.endDate || null,
      creditCardId: value.kind !== 'Income' && value.creditCardId > 0 ? value.creditCardId : null,
    };

    const editing = this.editing();
    this.run(editing ? this.api.update(editing.id, request, editing.version) : this.api.create(request), () =>
      this.editing.set(undefined),
    );
  }

  protected remove(item: RecurringCashFlow): void {
    if (!confirm(`"${item.name}" silinsin mi?`)) return;
    this.run(this.api.delete(item.id));
  }

  private reload(): void {
    this.loading.set(true);
    forkJoin({ items: this.api.list(), cards: this.cardsApi.list() }).subscribe({
      next: ({ items, cards }) => {
        this.items.set(items);
        this.cards.set(cards);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(problemMessage(err, 'Kayıtlar yüklenemedi.'));
        this.loading.set(false);
      },
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

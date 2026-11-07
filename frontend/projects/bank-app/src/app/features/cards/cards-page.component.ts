import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, forkJoin } from 'rxjs';

import { problemMessage } from '../../core/http/problem-details';
import { todayIsoDate } from '../../core/util/dates';
import { CreditCardsApi, InstallmentPlansApi } from '../../data-access/api/finance.api';
import { CreditCard, CurrencyCode, InstallmentPlan } from '../../data-access/models/finance.models';

@Component({
  selector: 'app-cards-page',
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe],
  templateUrl: './cards-page.component.html',
})
export class CardsPageComponent {
  private readonly cardsApi = inject(CreditCardsApi);
  private readonly plansApi = inject(InstallmentPlansApi);
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly cards = signal<CreditCard[]>([]);
  protected readonly plans = signal<InstallmentPlan[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  /** The card being edited, or null while adding a new one; undefined hides the form. */
  protected readonly editing = signal<CreditCard | null | undefined>(undefined);
  protected readonly addingPlan = signal(false);
  protected readonly expandedPlan = signal<number | null>(null);

  protected readonly plansByCard = computed(() => {
    const groups = new Map<number, InstallmentPlan[]>();
    for (const plan of this.plans()) groups.set(plan.creditCardId, [...(groups.get(plan.creditCardId) ?? []), plan]);
    return groups;
  });

  protected readonly cardForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    limitAmount: [0, [Validators.required, Validators.min(0.01)]],
    currencyCode: ['TRY' as CurrencyCode, Validators.required],
    statementDay: [20, [Validators.required, Validators.min(1), Validators.max(31)]],
    paymentDueDays: [10, [Validators.required, Validators.min(1), Validators.max(30)]],
  });

  protected readonly planForm = this.fb.group({
    creditCardId: [0, [Validators.required, Validators.min(1)]],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    totalAmount: [0, [Validators.required, Validators.min(0.01)]],
    installmentCount: [3, [Validators.required, Validators.min(1), Validators.max(36)]],
    purchaseDate: [todayIsoDate(), Validators.required],
  });

  constructor() {
    this.reload();
  }

  protected cardName(id: number): string {
    return this.cards().find((c) => c.id === id)?.name ?? '';
  }

  protected startAddingCard(): void {
    this.cardForm.reset();
    this.editing.set(null);
  }

  protected startEditingCard(card: CreditCard): void {
    this.cardForm.setValue({
      name: card.name,
      limitAmount: card.limitAmount,
      currencyCode: card.currencyCode,
      statementDay: card.statementDay,
      paymentDueDays: card.paymentDueDays,
    });
    this.editing.set(card);
  }

  protected saveCard(): void {
    this.cardForm.markAllAsTouched();
    if (this.cardForm.invalid) return;

    const editing = this.editing();
    const request = this.cardForm.getRawValue();
    const call = editing
      ? this.cardsApi.update(editing.id, request, editing.version)
      : this.cardsApi.create(request);

    this.run(call, () => this.editing.set(undefined));
  }

  protected deleteCard(card: CreditCard): void {
    if (!confirm(`"${card.name}" kartı silinsin mi?`)) return;
    this.run(this.cardsApi.delete(card.id));
  }

  protected startAddingPlan(): void {
    this.planForm.reset({ creditCardId: this.cards()[0]?.id ?? 0 });
    this.addingPlan.set(true);
  }

  protected savePlan(): void {
    this.planForm.markAllAsTouched();
    if (this.planForm.invalid) return;
    this.run(this.plansApi.create(this.planForm.getRawValue()), () => this.addingPlan.set(false));
  }

  protected deletePlan(plan: InstallmentPlan): void {
    if (!confirm(`"${plan.description}" taksit planı silinsin mi?`)) return;
    this.run(this.plansApi.delete(plan.id));
  }

  protected togglePlan(id: number): void {
    this.expandedPlan.update((current) => (current === id ? null : id));
  }

  private reload(): void {
    this.loading.set(true);
    forkJoin({ cards: this.cardsApi.list(), plans: this.plansApi.list() }).subscribe({
      next: ({ cards, plans }) => {
        this.cards.set(cards);
        this.plans.set(plans);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(problemMessage(err, 'Kartlar yüklenemedi.'));
        this.loading.set(false);
      },
    });
  }

  /** Runs a change, reloads on success, and on a 409 reloads too so the user sees the current data. */
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
        if (err instanceof HttpErrorResponse && err.status === 409 && this.editing()) {
          this.editing.set(undefined);
          this.reload();
        }
      },
    });
  }
}

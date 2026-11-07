import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';

import { problemMessage } from '../../core/http/problem-details';
import { CreditCardsApi, InstallmentPlansApi } from '../../data-access/api/finance.api';
import { CreditCard, CreditCardRequest, InstallmentPlan, InstallmentPlanRequest } from '../../data-access/models/finance.models';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { CardFormComponent } from './components/card-form.component';
import { CardListComponent } from './components/card-list.component';
import { InstallmentPlanFormComponent } from './components/installment-plan-form.component';
import { InstallmentPlanListComponent } from './components/installment-plan-list.component';

/** Container for cards and installment plans: loads both, runs every change and reloads afterwards. */
@Component({
  selector: 'app-cards-page',
  imports: [
    PageHeaderComponent,
    NoticeComponent,
    EmptyStateComponent,
    CardFormComponent,
    CardListComponent,
    InstallmentPlanFormComponent,
    InstallmentPlanListComponent,
  ],
  templateUrl: './cards-page.component.html',
})
export class CardsPageComponent {
  private readonly cardsApi = inject(CreditCardsApi);
  private readonly plansApi = inject(InstallmentPlansApi);

  protected readonly cards = signal<CreditCard[]>([]);
  protected readonly plans = signal<InstallmentPlan[]>([]);
  protected readonly loaded = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  /** The card being edited, null while adding one, undefined when the form is closed. */
  protected readonly editing = signal<CreditCard | null | undefined>(undefined);
  protected readonly addingPlan = signal(false);

  constructor() {
    this.reload();
  }

  protected saveCard(request: CreditCardRequest): void {
    const card = this.editing();
    const call = card ? this.cardsApi.update(card.id, request, card.version) : this.cardsApi.create(request);
    this.run(call, () => this.editing.set(undefined));
  }

  protected deleteCard(card: CreditCard): void {
    if (confirm(`"${card.name}" kartı silinsin mi?`)) this.run(this.cardsApi.delete(card.id));
  }

  protected savePlan(request: InstallmentPlanRequest): void {
    this.run(this.plansApi.create(request), () => this.addingPlan.set(false));
  }

  protected deletePlan(plan: InstallmentPlan): void {
    if (confirm(`"${plan.description}" taksit planı silinsin mi?`)) this.run(this.plansApi.delete(plan.id));
  }

  private reload(): void {
    forkJoin({ cards: this.cardsApi.list(), plans: this.plansApi.list() }).subscribe({
      next: ({ cards, plans }) => {
        this.cards.set(cards);
        this.plans.set(plans);
        this.loaded.set(true);
      },
      error: (err) => this.error.set(problemMessage(err, 'Kartlar yüklenemedi.')),
    });
  }

  /** Runs a change and reloads. A 409 means the data moved underneath the user, so it reloads too. */
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

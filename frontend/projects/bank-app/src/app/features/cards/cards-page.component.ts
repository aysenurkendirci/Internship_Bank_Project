import { Component, inject, signal } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';

import { ActOptions, BasePage } from '../../core/page/base-page';
import { ConfirmService } from '../../core/ui/confirm.service';
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
export class CardsPageComponent extends BasePage {
  private readonly dialog = inject(ConfirmService);
  private readonly cardsApi = inject(CreditCardsApi);
  private readonly plansApi = inject(InstallmentPlansApi);

  protected readonly cards = signal<CreditCard[]>([]);
  protected readonly plans = signal<InstallmentPlan[]>([]);
  protected readonly loaded = signal(false);

  /** The card being edited, null while adding one, undefined when the form is closed. */
  protected readonly editing = signal<CreditCard | null | undefined>(undefined);
  protected readonly addingPlan = signal(false);

  constructor() {
    super();
    this.reload();
  }

  protected saveCard(request: CreditCardRequest): void {
    const card = this.editing();
    const call = card ? this.cardsApi.update(card.id, request, card.version) : this.cardsApi.create(request);
    this.run(call, () => this.editing.set(undefined));
  }

  protected deleteCard(card: CreditCard): void {
    this.dialog.confirmDelete(`"${card.name}" kartı silinsin mi?`).then((yes) => yes && this.run(this.cardsApi.delete(card.id)));
  }

  protected savePlan(request: InstallmentPlanRequest): void {
    this.run(this.plansApi.create(request), () => this.addingPlan.set(false));
  }

  protected deletePlan(plan: InstallmentPlan): void {
    this.dialog.confirmDelete(`"${plan.description}" taksit planı silinsin mi?`).then((yes) => yes && this.run(this.plansApi.delete(plan.id)));
  }

  private reload(): void {
    this.load(
      forkJoin({ cards: this.cardsApi.list(), plans: this.plansApi.list() }),
      ({ cards, plans }) => {
        this.cards.set(cards);
        this.plans.set(plans);
        this.loaded.set(true);
      },
      'Kartlar yüklenemedi.',
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

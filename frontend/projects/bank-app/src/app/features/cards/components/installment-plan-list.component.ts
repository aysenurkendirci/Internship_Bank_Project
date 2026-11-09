import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

import { CreditCard, InstallmentPlan } from '../../../data-access/models/finance.models';

/** Installment purchases; each one opens to its statement and due dates. */
@Component({
  selector: 'app-installment-plan-list',
  imports: [CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="space-y-3">
      @for (plan of plans(); track plan.id) {
        <li class="panel">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <div>
              <p class="font-semibold text-ink">{{ plan.description }}</p>
              <p class="text-sm font-semibold text-ink-soft">
                {{ cardName(plan.creditCardId) }} · {{ plan.purchaseDate | date: 'd MMMM y' }} · {{ plan.installmentCount }} taksit
              </p>
            </div>
            <div class="flex items-center gap-4">
              <p class="font-semibold text-ink">{{ plan.totalAmount | currency: plan.currencyCode : 'symbol-narrow' }}</p>
              <button type="button" class="btn-link" (click)="toggle(plan.id)" [attr.aria-expanded]="expanded() === plan.id">
                {{ expanded() === plan.id ? 'Takvimi gizle' : 'Takvim' }}
              </button>
              <button type="button" class="btn-link text-danger" (click)="remove.emit(plan)" [disabled]="busy()">Sil</button>
            </div>
          </div>

          @if (expanded() === plan.id) {
            <table class="w-full mt-4 text-sm">
              <thead class="text-left text-ink-faint text-xs">
                <tr><th class="py-2">#</th><th>Ekstre</th><th>Son ödeme</th><th class="text-right">Tutar</th></tr>
              </thead>
              <tbody>
                @for (installment of plan.installments; track installment.number) {
                  <tr class="border-t border-line font-semibold text-ink-soft">
                    <td class="py-2">{{ installment.number }}</td>
                    <td>{{ installment.statementDate | date: 'd MMM y' }}</td>
                    <td>{{ installment.dueDate | date: 'd MMM y' }}</td>
                    <td class="text-right">{{ installment.amount | currency: plan.currencyCode : 'symbol-narrow' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }
        </li>
      }
    </ul>
  `,
})
export class InstallmentPlanListComponent {
  readonly plans = input.required<readonly InstallmentPlan[]>();
  readonly cards = input.required<readonly CreditCard[]>();
  readonly busy = input(false);
  readonly remove = output<InstallmentPlan>();

  protected readonly expanded = signal<number | null>(null);

  protected cardName(id: number): string {
    return this.cards().find((c) => c.id === id)?.name ?? '';
  }

  protected toggle(id: number): void {
    this.expanded.update((current) => (current === id ? null : id));
  }
}

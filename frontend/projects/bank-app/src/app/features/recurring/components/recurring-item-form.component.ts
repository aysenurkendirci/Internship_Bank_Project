import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { todayIsoDate } from '../../../core/util/dates';
import {
  CashFlowKind,
  CreditCard,
  CurrencyCode,
  Frequency,
  RecurringCashFlow,
  RecurringCashFlowRequest,
} from '../../../data-access/models/finance.models';
import { KIND_LABELS } from '../recurring-labels';

@Component({
  selector: 'app-recurring-item-form',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './recurring-item-form.component.html',
})
export class RecurringItemFormComponent {
  readonly item = input<RecurringCashFlow | null>(null);
  readonly cards = input.required<readonly CreditCard[]>();
  readonly busy = input(false);
  readonly save = output<RecurringCashFlowRequest>();
  readonly cancel = output<void>();

  protected readonly kindLabels = KIND_LABELS;
  protected readonly kinds = Object.keys(KIND_LABELS) as CashFlowKind[];

  protected readonly form = inject(FormBuilder).nonNullable.group({
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

  // Income can never be charged to a card and the API would reject it, so the field is not offered.
  private readonly kind = toSignal(this.form.controls.kind.valueChanges, { initialValue: this.form.controls.kind.value });
  protected readonly canUseCard = computed(() => this.kind() !== 'Income');

  constructor() {
    effect(() => {
      const item = this.item();
      if (!item) {
        this.form.reset();
        return;
      }
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
    });
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    this.save.emit({
      ...value,
      endDate: value.endDate || null,
      creditCardId: value.kind !== 'Income' && value.creditCardId > 0 ? value.creditCardId : null,
    });
  }
}

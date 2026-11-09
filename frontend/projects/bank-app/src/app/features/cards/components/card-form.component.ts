import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CreditCard, CreditCardRequest, CurrencyCode } from '../../../data-access/models/finance.models';
import { BaseForm } from '../../../shared/forms/base-form';

/** Add or edit a card. Given a card it pre-fills; given null it starts empty. */
@Component({
  selector: 'app-card-form',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form class="panel grid gap-4 sm:grid-cols-2 lg:grid-cols-5" [formGroup]="form" (ngSubmit)="submit()">
      <label class="field lg:col-span-2">
        <span class="field-label">Kart adı</span>
        <input class="input" formControlName="name" placeholder="Örn. Bonus" />
        @if (form.controls.name.touched && form.controls.name.invalid) {
          <span class="field-error">Kart adı zorunludur.</span>
        }
      </label>
      <label class="field">
        <span class="field-label">Limit</span>
        <input class="input" type="number" min="0" step="0.01" formControlName="limitAmount" />
      </label>
      <label class="field">
        <span class="field-label">Kesim günü</span>
        <input class="input" type="number" min="1" max="31" formControlName="statementDay" />
      </label>
      <label class="field">
        <span class="field-label">Son ödeme (gün)</span>
        <input class="input" type="number" min="1" max="30" formControlName="paymentDueDays" />
      </label>
      <label class="field">
        <span class="field-label">Para birimi</span>
        <select class="input" formControlName="currencyCode">
          <option value="TRY">TRY</option>
          <option value="USD">USD</option>
          <option value="EUR">EUR</option>
        </select>
      </label>
      <div class="flex items-end gap-2 lg:col-span-4">
        <button type="submit" class="btn-primary" [disabled]="busy()">{{ card() ? 'Güncelle' : 'Kaydet' }}</button>
        <button type="button" class="btn-secondary" (click)="cancel.emit()">Vazgeç</button>
      </div>
    </form>
  `,
})
export class CardFormComponent extends BaseForm<CreditCardRequest> {
  readonly card = input<CreditCard | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    limitAmount: [0, [Validators.required, Validators.min(0.01)]],
    currencyCode: ['TRY' as CurrencyCode, Validators.required],
    statementDay: [20, [Validators.required, Validators.min(1), Validators.max(31)]],
    paymentDueDays: [10, [Validators.required, Validators.min(1), Validators.max(30)]],
  });

  constructor() {
    super();
    effect(() => {
      const card = this.card();
      if (card) {
        const { name, limitAmount, currencyCode, statementDay, paymentDueDays } = card;
        this.form.setValue({ name, limitAmount, currencyCode, statementDay, paymentDueDays });
      } else {
        this.form.reset();
      }
    });
  }

  protected toRequest(): CreditCardRequest {
    return this.form.getRawValue();
  }
}

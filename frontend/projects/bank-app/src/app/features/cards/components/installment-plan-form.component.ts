import { ChangeDetectionStrategy, Component, OnInit, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { todayIsoDate } from '../../../core/util/dates';
import { CreditCard, InstallmentPlanRequest } from '../../../data-access/models/finance.models';
import { BaseForm } from '../../../shared/forms/base-form';

/** Records a purchase paid in installments. The currency comes from the chosen card. */
@Component({
  selector: 'app-installment-plan-form',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form class="panel grid gap-4 sm:grid-cols-2 lg:grid-cols-5" [formGroup]="form" (ngSubmit)="submit()">
      <label class="field">
        <span class="field-label">Kart</span>
        <select class="input" formControlName="creditCardId">
          @for (card of cards(); track card.id) {
            <option [ngValue]="card.id">{{ card.name }}</option>
          }
        </select>
      </label>
      <label class="field lg:col-span-2">
        <span class="field-label">Açıklama</span>
        <input class="input" formControlName="description" placeholder="Örn. Dizüstü bilgisayar" />
        @if (form.controls.description.touched && form.controls.description.invalid) {
          <span class="field-error">Açıklama zorunludur.</span>
        }
      </label>
      <label class="field">
        <span class="field-label">Toplam tutar</span>
        <input class="input" type="number" min="0" step="0.01" formControlName="totalAmount" />
      </label>
      <label class="field">
        <span class="field-label">Taksit sayısı</span>
        <input class="input" type="number" min="1" max="36" formControlName="installmentCount" />
      </label>
      <label class="field">
        <span class="field-label">Alışveriş tarihi</span>
        <input class="input" type="date" formControlName="purchaseDate" />
      </label>
      <div class="flex items-end gap-2 lg:col-span-4">
        <button type="submit" class="btn-primary" [disabled]="busy()">Kaydet</button>
        <button type="button" class="btn-secondary" (click)="cancel.emit()">Vazgeç</button>
      </div>
    </form>
  `,
})
export class InstallmentPlanFormComponent extends BaseForm<InstallmentPlanRequest> implements OnInit {
  readonly cards = input.required<readonly CreditCard[]>();

  protected readonly form = inject(FormBuilder).nonNullable.group({
    creditCardId: [0, [Validators.required, Validators.min(1)]],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    totalAmount: [0, [Validators.required, Validators.min(0.01)]],
    installmentCount: [3, [Validators.required, Validators.min(1), Validators.max(36)]],
    purchaseDate: [todayIsoDate(), Validators.required],
  });

  ngOnInit(): void {
    this.form.controls.creditCardId.setValue(this.cards()[0]?.id ?? 0);
  }

  protected toRequest(): InstallmentPlanRequest {
    return this.form.getRawValue();
  }
}

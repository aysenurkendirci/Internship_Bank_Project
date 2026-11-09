import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CardSettingsRequest } from '../../../data-access/models/banking.models';
import { CardItem } from '../../../data-access/models/dashboard.models';
import { BaseForm } from '../../../shared/forms/base-form';

const LIMIT_RULES = [Validators.required, Validators.min(0), Validators.max(10_000_000)];

/** Switches and limits of one card. Settings of a closed card stay visible but cannot be changed. */
@Component({
  selector: 'app-card-settings',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <form class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
      <label class="flex items-center justify-between gap-4">
        <span class="text-sm text-ink">Temassız ödeme</span>
        <input type="checkbox" role="switch" class="switch" formControlName="contactless" />
      </label>
      <label class="flex items-center justify-between gap-4">
        <span class="text-sm text-ink">İnternetten alışveriş</span>
        <input type="checkbox" role="switch" class="switch" formControlName="onlineUse" />
      </label>

      <div class="grid grid-cols-2 gap-3">
        <label class="field">
          <span class="field-label">Günlük limit</span>
          <input class="input tabular-nums" type="number" min="0" step="100" formControlName="dailyLimit" />
        </label>
        <label class="field">
          <span class="field-label">Aylık limit</span>
          <input class="input tabular-nums" type="number" min="0" step="100" formControlName="monthlyLimit" />
        </label>
      </div>
      @if (form.controls.dailyLimit.invalid || form.controls.monthlyLimit.invalid) {
        @if (!locked()) {
          <p class="field-error">Limitler 0 ile 10.000.000 arasında olmalıdır.</p>
        }
      }

      <div class="flex flex-wrap gap-2">
        <button type="submit" class="btn-primary" [disabled]="busy() || locked()">Kaydet</button>
        @if (card().status !== 'Cancelled') {
          <button type="button" [class]="card().status === 'Blocked' ? 'btn-secondary' : 'btn-danger'" [disabled]="busy()" (click)="toggleBlock.emit()">
            {{ card().status === 'Blocked' ? 'Kartı aç' : 'Geçici olarak kapat' }}
          </button>
        }
      </div>
    </form>
  `,
})
export class CardSettingsComponent extends BaseForm<CardSettingsRequest> {
  readonly card = input.required<CardItem>();
  readonly toggleBlock = output<void>();

  protected readonly locked = computed(() => this.card().status !== 'Active');

  protected readonly form = inject(FormBuilder).nonNullable.group({
    contactless: [false],
    onlineUse: [false],
    dailyLimit: [0, LIMIT_RULES],
    monthlyLimit: [0, LIMIT_RULES],
  });

  constructor() {
    super();
    effect(() => {
      const card = this.card();
      this.form.reset({
        contactless: card.settings?.contactless ?? false,
        onlineUse: card.settings?.onlineUse ?? false,
        dailyLimit: card.limits?.dailyLimit ?? 0,
        monthlyLimit: card.limits?.monthlyLimit ?? 0,
      });
      if (this.locked()) this.form.disable({ emitEvent: false });
      else this.form.enable({ emitEvent: false });
    });
  }

  protected toRequest(): CardSettingsRequest {
    return this.form.getRawValue();
  }
}

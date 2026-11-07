import { ChangeDetectionStrategy, Component, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { todayIsoDate } from '../../../core/util/dates';
import { CreditCard, InstallmentScenarioRequest } from '../../../data-access/models/finance.models';

const MAX_OPTIONS = 6;

/** The planned purchase and its payment options; emits a ready request when the user compares. */
@Component({
  selector: 'app-scenario-form',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './scenario-form.component.html',
})
export class ScenarioFormComponent {
  readonly cards = input.required<readonly CreditCard[]>();
  readonly busy = input(false);
  readonly compare = output<InstallmentScenarioRequest>();

  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly maxOptions = MAX_OPTIONS;

  protected readonly form = this.fb.group({
    creditCardId: [0, [Validators.required, Validators.min(1)]],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    purchaseDate: [todayIsoDate(), Validators.required],
    options: this.fb.array([this.option(3), this.option(6)]),
  });

  constructor() {
    // Preselect the first card once the list arrives.
    effect(() => {
      const first = this.cards()[0];
      if (first && this.form.controls.creditCardId.value === 0) this.form.controls.creditCardId.setValue(first.id);
    });
  }

  protected get options() {
    return this.form.controls.options;
  }

  protected addOption(): void {
    if (this.options.length >= MAX_OPTIONS) return;
    const lastAmount = this.options.at(this.options.length - 1)?.value.totalAmount ?? 0;
    this.options.push(this.option(9, lastAmount));
  }

  protected removeOption(index: number): void {
    if (this.options.length > 1) this.options.removeAt(index);
  }

  protected copyPriceToAll(): void {
    const price = this.options.at(0).value.totalAmount;
    for (const control of this.options.controls) control.patchValue({ totalAmount: price });
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    // The API stretches the horizon to the longest option; 3 months is only the minimum.
    this.compare.emit({ ...this.form.getRawValue(), months: 3 });
  }

  private option(installmentCount: number, totalAmount = 0) {
    return this.fb.group({
      installmentCount: [installmentCount, [Validators.required, Validators.min(1), Validators.max(36)]],
      totalAmount: [totalAmount, [Validators.required, Validators.min(0.01)]],
    });
  }
}

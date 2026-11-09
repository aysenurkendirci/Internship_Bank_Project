import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';

import { PasswordChange } from '../../../data-access/models/banking.models';
import { BaseForm } from '../../../shared/forms/base-form';

const sameAsNew = (group: AbstractControl): ValidationErrors | null =>
  group.get('newPassword')?.value === group.get('repeat')?.value ? null : { mismatch: true };

/** Changing the password asks for the current one, and for the new one twice. */
@Component({
  selector: 'app-password-form',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <form class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
      <label class="field">
        <span class="field-label">Mevcut şifre</span>
        <input class="input" type="password" formControlName="currentPassword" autocomplete="current-password" />
        @if (form.controls.currentPassword.touched && form.controls.currentPassword.invalid) {
          <span class="field-error">Mevcut şifrenizi girin.</span>
        }
      </label>
      <label class="field">
        <span class="field-label">Yeni şifre</span>
        <input class="input" type="password" formControlName="newPassword" autocomplete="new-password" />
        @if (form.controls.newPassword.touched && form.controls.newPassword.invalid) {
          <span class="field-error">En az 8 karakter olmalıdır.</span>
        }
      </label>
      <label class="field">
        <span class="field-label">Yeni şifre (tekrar)</span>
        <input class="input" type="password" formControlName="repeat" autocomplete="new-password" />
        @if (form.controls.repeat.touched && form.hasError('mismatch')) {
          <span class="field-error">Şifreler aynı değil.</span>
        }
      </label>
      <button type="submit" class="btn-primary" [disabled]="busy()">Şifreyi değiştir</button>
    </form>
  `,
})
export class PasswordFormComponent extends BaseForm<PasswordChange> {
  protected readonly form = inject(FormBuilder).nonNullable.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
      repeat: ['', Validators.required],
    },
    { validators: sameAsNew },
  );

  /** Called by the page after a successful change, so passwords do not linger in the fields. */
  clear(): void {
    this.form.reset();
  }

  protected toRequest(): PasswordChange {
    const { currentPassword, newPassword } = this.form.getRawValue();
    return { currentPassword, newPassword };
  }
}

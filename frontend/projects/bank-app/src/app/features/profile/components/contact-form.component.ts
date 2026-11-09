import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ContactDetails, Profile } from '../../../data-access/models/banking.models';
import { BaseForm } from '../../../shared/forms/base-form';

/** E-mail and phone, the details a customer may change without visiting a branch. */
@Component({
  selector: 'app-contact-form',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <form class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
      <label class="field">
        <span class="field-label">E-posta</span>
        <input class="input" type="email" formControlName="email" autocomplete="email" />
        @if (form.controls.email.touched && form.controls.email.invalid) {
          <span class="field-error">Geçerli bir e-posta girin.</span>
        }
      </label>
      <label class="field">
        <span class="field-label">Telefon</span>
        <input class="input" type="tel" formControlName="phone" autocomplete="tel" maxlength="20" />
        @if (form.controls.phone.touched && form.controls.phone.invalid) {
          <span class="field-error">Telefon zorunludur.</span>
        }
      </label>
      <button type="submit" class="btn-primary" [disabled]="busy()">Kaydet</button>
    </form>
  `,
})
export class ContactFormComponent extends BaseForm<ContactDetails> {
  readonly profile = input.required<Profile>();

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    phone: ['', Validators.required],
  });

  constructor() {
    super();
    effect(() => {
      const { email, phone } = this.profile();
      this.form.reset({ email, phone });
    });
  }

  protected toRequest(): ContactDetails {
    const { email, phone } = this.form.getRawValue();
    return { email: email.trim(), phone: phone.trim() };
  }
}

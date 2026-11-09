import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { BasePage } from '../../../../core/page/base-page';
import { AuthApi } from '../../../../data-access/api/auth.api';
import { AuthShellComponent } from '../../../../layout/auth/auth-shell.component';
import { NoticeComponent } from '../../../../shared/ui/notice.component';

type Membership = 'Personal' | 'Corporate';

/** Creates the account, then sends the customer to sign in with the identity number already filled in. */
@Component({
  imports: [ReactiveFormsModule, RouterLink, AuthShellComponent, NoticeComponent],
  templateUrl: './register.component.html',
})
export class RegisterComponent extends BasePage {
  private readonly auth = inject(AuthApi);
  private readonly router = inject(Router);

  protected readonly memberships: readonly { value: Membership; label: string }[] = [
    { value: 'Personal', label: 'Bireysel' },
    { value: 'Corporate', label: 'Kurumsal' },
  ];

  protected readonly form = inject(FormBuilder).nonNullable.group({
    tcNo: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    membership: ['Personal' as Membership],
  });

  protected show(field: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[field];
    return control.touched && control.invalid;
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.busy()) return;

    const payload = this.form.getRawValue();
    this.act(this.auth.register(payload), {
      fallback: 'Kayıt tamamlanamadı.',
      onDone: () => this.router.navigate(['/auth/login'], { queryParams: { registered: '1', tcNo: payload.tcNo } }),
    });
  }
}

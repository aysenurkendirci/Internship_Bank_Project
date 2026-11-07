import { Component, inject, signal } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';

import { problemMessage } from '../../../../core/http/problem-details';
import { AuthApi } from '../../../../data-access/api/auth.api';

import { InputComponent } from '../../../../shared/input/input.component';
import { ButtonComponent } from '../../../../shared/button/button.component';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, InputComponent, ButtonComponent],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthApi);
  private router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    tcNo: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    membership: ['Personal' as 'Personal' | 'Corporate'],
  });

  setMembership(v: 'Personal' | 'Corporate') {
    this.form.controls.membership.setValue(v);
  }

  submit() {
    // Touch first so every invalid field shows its message, then stop.
    this.form.markAllAsTouched();
    if (this.form.invalid || this.loading()) return;

    this.loading.set(true);
    this.error.set(null);

    const payload = this.form.getRawValue();

    this.auth
      .register(payload)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () =>
          this.router.navigate(['/auth/login'], { queryParams: { registered: '1', tcNo: payload.tcNo } }),
        error: (err) => this.error.set(problemMessage(err, 'Kayıt tamamlanamadı.')),
      });
  }
}

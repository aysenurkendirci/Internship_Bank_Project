import { Component, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize, switchMap } from 'rxjs';
import { AuthService } from '../../../../core/auth/auth.service';

import { InputComponent } from '../../../../shared/input/input.component';
import { ButtonComponent } from '../../../../shared/button/button.component';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, InputComponent, ButtonComponent],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'], // ✅ DÜZELTİLDİ
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = false;
  error?: string;

  form = this.fb.group({
    tcNo: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    password: ['', [Validators.required, Validators.minLength(6)]],
    membership: ['Personal' as 'Personal' | 'Corporate'],
  });

  setMembership(v: 'Personal' | 'Corporate') {
    this.form.controls.membership.setValue(v);
  }

  submit() {
    if (this.form.invalid || this.loading) return;

    this.loading = true;
    this.error = undefined;

    const payload = this.form.getRawValue() as any;

    this.auth
      .register(payload)
      .pipe(
        switchMap(() =>
          this.auth.login({ tcNo: payload.tcNo, password: payload.password } as any)
        ),
        finalize(() => (this.loading = false))
      )
      .subscribe({
        next: () => this.router.navigateByUrl('/dashboard'),
        error: (err: any) => {
          this.error = err?.error?.message ?? err?.message ?? 'Kayıt başarısız.';
        },
      });
  }
}

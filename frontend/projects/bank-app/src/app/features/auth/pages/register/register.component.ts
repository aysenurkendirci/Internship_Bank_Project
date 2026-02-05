import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { InputComponent } from '../../../../shared/input/input.component';
import { ButtonComponent } from '../../../../shared/button/button.component';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, InputComponent, ButtonComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = false;
  error?: string;

  // UI’daki alanlara göre
  form = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    tcNo: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, Validators.minLength(10)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    membership: ['Bireysel', [Validators.required]], // Bireysel | Kurumsal
  });

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.loading) return;

    this.loading = true;
    this.error = undefined;

    // 🔧 Backend endpoint’in: auth.register(...) gibi ise onu çağır
    // Burada varsayım: auth.register(payload) var.
    const payload = this.form.getRawValue();

    this.auth
      .register(payload as any)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: () => {
          // Kayıt sonrası login’e success param ile dön
          this.router.navigate(['/auth/login'], {
            queryParams: { registered: '1', tcNo: payload.tcNo },
          });
        },
        error: (err: any) => {
          this.error = err?.error?.message ?? err?.message ?? 'Kayıt başarısız';
        },
      });
  }

  setMembership(type: 'Bireysel' | 'Kurumsal') {
    this.form.controls.membership.setValue(type);
  }

  // UI error helpers
  isInvalid(name: keyof typeof this.form.controls) {
    const c = this.form.controls[name];
    return c.touched && c.invalid;
  }
}

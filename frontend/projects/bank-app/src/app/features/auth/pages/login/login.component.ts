import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';

import { SessionService } from '../../../../core/auth/session.service';
import { problemMessage } from '../../../../core/http/problem-details';
import { AuthApi } from '../../../../data-access/api/auth.api';
import { ButtonComponent } from '../../../../shared/button/button.component';
import { InputComponent } from '../../../../shared/input/input.component';

@Component({
  imports: [ReactiveFormsModule, RouterModule, InputComponent, ButtonComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthApi);
  private readonly session = inject(SessionService);
  private readonly router = inject(Router);
  private readonly params = inject(ActivatedRoute).snapshot.queryParamMap;

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(this.initialNotice());

  // Only presence is checked: the API deliberately does not say which part of a login was wrong.
  protected readonly form = this.fb.nonNullable.group({
    tcNo: [this.params.get('tcNo') ?? '', Validators.required],
    password: ['', Validators.required],
  });

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.loading()) return;

    this.loading.set(true);
    this.error.set(null);

    this.auth
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.session.start(response);
          this.router.navigateByUrl(this.safeReturnUrl());
        },
        error: (err) => this.error.set(problemMessage(err, 'Giriş yapılamadı.')),
      });
  }

  private initialNotice(): string | null {
    if (this.params.get('registered') === '1') return 'Hesabınız oluşturuldu. Giriş yapabilirsiniz.';
    if (this.params.get('expired') === '1') return 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.';
    return null;
  }

  // Only in-app paths are followed, so a crafted link cannot send the user to another site after login.
  private safeReturnUrl(): string {
    const url = this.params.get('returnUrl');
    return url && url.startsWith('/') && !url.startsWith('//') ? url : '/dashboard';
  }
}

import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { SessionService } from '../../../../core/auth/session.service';
import { BasePage } from '../../../../core/page/base-page';
import { AuthApi } from '../../../../data-access/api/auth.api';
import { AuthShellComponent } from '../../../../layout/auth/auth-shell.component';
import { NoticeComponent } from '../../../../shared/ui/notice.component';

/** Signs the customer in and takes them back to the page they came from. */
@Component({
  imports: [ReactiveFormsModule, RouterLink, AuthShellComponent, NoticeComponent],
  templateUrl: './login.component.html',
})
export class LoginComponent extends BasePage {
  private readonly auth = inject(AuthApi);
  private readonly session = inject(SessionService);
  private readonly router = inject(Router);
  private readonly params = inject(ActivatedRoute).snapshot.queryParamMap;

  protected readonly notice = signal<string | null>(this.initialNotice());

  // Only presence is checked: the API deliberately does not say which part of a login was wrong.
  protected readonly form = inject(FormBuilder).nonNullable.group({
    tcNo: [this.params.get('tcNo') ?? '', Validators.required],
    password: ['', Validators.required],
  });

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.busy()) return;

    this.act(this.auth.login(this.form.getRawValue()), {
      fallback: 'Giriş yapılamadı.',
      onDone: (response) => {
        this.session.start(response);
        this.router.navigateByUrl(this.safeReturnUrl());
      },
    });
  }

  private initialNotice(): string | null {
    if (this.params.get('registered') === '1') return 'Hesabınız oluşturuldu. Giriş yapabilirsiniz.';
    if (this.params.get('expired') === '1') return 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.';
    if (this.params.get('erased') === '1') return 'Hesabınız silindi.';
    return null;
  }

  // Only in-app paths are followed, so a crafted link cannot send the user to another site after login.
  private safeReturnUrl(): string {
    const url = this.params.get('returnUrl');
    return url && url.startsWith('/') && !url.startsWith('//') ? url : '/dashboard';
  }
}

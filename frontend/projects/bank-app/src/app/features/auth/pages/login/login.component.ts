import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/auth/auth.service';

import { InputComponent } from '../../../../shared/input/input.component';
import { ButtonComponent } from '../../../../shared/button/button.component';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, InputComponent, ButtonComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loading = false;
  error?: string;
  success?: string;

  form = this.fb.group({
    tcNo: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  ngOnInit() {
    this.route.queryParamMap.subscribe(params => {
      if (params.get('registered') === '1') {
        this.success = params.get('message') ?? 'Başarıyla hesabınız oluşturuldu. VBank’a hoş geldiniz!';
      }
      const tcNo = params.get('tcNo');
      if (tcNo) this.form.controls.tcNo.setValue(tcNo);
    });
  }

  submit() {
    if (this.form.invalid || this.loading) return;
    this.form.markAllAsTouched();

    this.loading = true;
    this.error = undefined;

    this.auth
      .login(this.form.getRawValue() as any)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (res: any) => {
          // Token kontrolü ve saklanması
          const token = res?.token ?? res?.jwt;

          if (token) {
            localStorage.setItem('token', token);
          } else {
            console.warn('Login response içinde token/jwt bulunamadı:', res);
          }

          this.router.navigateByUrl('/dashboard');
        },
        error: (err: any) => {
          this.error = err?.error?.message ?? err?.message ?? 'Giriş başarısız';
        },
      });
  }
}
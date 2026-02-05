import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');

  const authReq = token
    ? req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
      })
    : req;

  return next(authReq).pipe(
    catchError((err) => {
      if (err?.status === 401) {
        // token geçersiz/expired ise temizle ve login'e dön
        localStorage.removeItem('token');
        localStorage.removeItem('fullName');
        router.navigateByUrl('/auth/login');
      }
      return throwError(() => err);
    })
  );
};

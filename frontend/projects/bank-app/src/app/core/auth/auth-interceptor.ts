import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { SessionService } from './session.service';

/**
 * Attaches the access token and ends the session when the API rejects it (expired or revoked), sending
 * the user to the login page with a way back to where they were.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(SessionService);
  const router = inject(Router);
  const token = session.token();

  const request = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(request).pipe(
    catchError((error: unknown) => {
      // A 401 from the login endpoint means wrong credentials, which the login form reports itself.
      if (error instanceof HttpErrorResponse && error.status === 401 && token) {
        session.end();
        router.navigate(['/auth/login'], { queryParams: { returnUrl: router.url, expired: '1' } });
      }
      return throwError(() => error);
    }),
  );
};

import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, of, switchMap, throwError } from 'rxjs';

import { SessionService } from './session.service';

/**
 * Attaches the access token. When the API rejects it, gets a new one once and repeats the request, so an expired
 * token is invisible to the user. Only when that fails does the session end. A repeated transfer keeps its
 * Idempotency-Key, so it cannot move the money twice.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(SessionService);
  const router = inject(Router);
  const sentToken = session.token();

  const send = (token: string | null) =>
    next(token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);

  return send(sentToken).pipe(
    catchError((error: unknown) => {
      // A 401 from /api/auth is about the credentials or the cookie itself; a new token cannot change the answer.
      const canRenew = error instanceof HttpErrorResponse && error.status === 401 && sentToken && !req.url.startsWith('/api/auth/');
      if (!canRenew) return throwError(() => error);

      return freshToken(session, sentToken).pipe(
        catchError((renewal: unknown) => {
          // Only the server saying the session is over ends it. A network blip must not sign anyone out.
          if (!(renewal instanceof HttpErrorResponse) || renewal.status !== 401) return throwError(() => renewal);

          session.end();
          router.navigate(['/auth/login'], { queryParams: { returnUrl: router.url, expired: '1' } });
          return throwError(() => error);
        }),
        switchMap((token) => send(token)),
      );
    }),
  );
};

/** Another request may already have renewed the token while this one was on its way. */
function freshToken(session: SessionService, sentToken: string): Observable<string> {
  const current = session.token();
  return current && current !== sentToken ? of(current) : session.refresh();
}

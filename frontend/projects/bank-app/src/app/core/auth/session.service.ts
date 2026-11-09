import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, firstValueFrom, from, map, of, shareReplay, tap } from 'rxjs';

/** What the session needs from a sign-in. Declared here so core does not depend on the API models. */
export interface SessionUser {
  token: string;
  fullName: string;
}

// Says only that a sign-in happened in this browser, so anonymous visitors are not sent to /refresh for a 401.
const SIGNED_IN_HINT = 'signedIn';

// The API refuses cookie-authenticated calls without this header. A cross-site form cannot set it (CSRF).
const APP_HEADERS = new HttpHeaders({ 'X-Requested-By': 'bank-web' });

/**
 * The single owner of the signed-in state.
 *
 * The access token lives only in memory, so injected script finds nothing in storage and it is gone when the tab
 * closes. It is short-lived; the HttpOnly refresh cookie, which script cannot read, buys a new one when it runs
 * out and after a page reload.
 */
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly http = inject(HttpClient);

  private readonly tokenSignal = signal<string | null>(null);
  private readonly nameSignal = signal<string | null>(null);
  private refreshing: Observable<string> | null = null;

  readonly token = this.tokenSignal.asReadonly();
  readonly fullName = this.nameSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.tokenSignal() !== null);

  start(user: SessionUser): void {
    localStorage.setItem(SIGNED_IN_HINT, '1');
    this.tokenSignal.set(user.token);
    this.nameSignal.set(user.fullName);
  }

  end(): void {
    localStorage.removeItem(SIGNED_IN_HINT);
    this.tokenSignal.set(null);
    this.nameSignal.set(null);
  }

  /** Runs before the first route is resolved, so a page reload does not land on the login screen. */
  restore(): Promise<unknown> {
    // Browsers that signed in before tokens moved to memory still hold a long-lived copy.
    localStorage.removeItem('token');
    localStorage.removeItem('fullName');

    if (localStorage.getItem(SIGNED_IN_HINT) === null) return Promise.resolve();
    return firstValueFrom(this.refresh()).catch((error: unknown) => {
      // The hint stays after a passing failure, so the next load tries again instead of forgetting the session.
      if (error instanceof HttpErrorResponse && error.status === 401) this.end();
    });
  }

  /**
   * Trades the refresh cookie for a new access token. Requests that fail together share one exchange: the cookie
   * is single-use, and presenting a used one reads as theft and ends every session.
   */
  refresh(): Observable<string> {
    this.refreshing ??= from(this.exchangeCookie()).pipe(
      tap((user) => this.start(user)),
      map((user) => user.token),
      finalize(() => (this.refreshing = null)),
      shareReplay(1),
    );
    return this.refreshing;
  }

  /** Revokes the refresh token on the server. The local session ends even if that call fails. */
  signOut(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', null, { headers: APP_HEADERS }).pipe(
      catchError(() => of(undefined)),
      tap(() => this.end()),
    );
  }

  private async exchangeCookie(): Promise<SessionUser> {
    const call = () => firstValueFrom(this.http.post<SessionUser>('/api/auth/refresh', null, { headers: APP_HEADERS }));

    // Open tabs share the cookie, so they take turns; the later tab then sends the cookie the earlier one received.
    return navigator.locks ? await navigator.locks.request('session-refresh', call) : call();
  }
}

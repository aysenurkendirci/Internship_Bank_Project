import { Injectable, computed, signal } from '@angular/core';

/** What the session needs from a sign-in. Declared here so core does not depend on the API models. */
export interface SessionUser {
  token: string;
  fullName: string;
}

const TOKEN_KEY = 'token';
const NAME_KEY = 'fullName';

/**
 * The single owner of the signed-in state. Everything else asks this service instead of reading
 * storage directly, so the storage choice can change in one place.
 *
 * The access token is kept in localStorage, which script injected into the page could read. The
 * planned hardening is a short-lived in-memory access token with an HttpOnly refresh cookie.
 */
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly tokenSignal = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  private readonly nameSignal = signal<string | null>(localStorage.getItem(NAME_KEY));

  readonly token = this.tokenSignal.asReadonly();
  readonly fullName = this.nameSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.tokenSignal() !== null);

  start(auth: SessionUser): void {
    localStorage.setItem(TOKEN_KEY, auth.token);
    localStorage.setItem(NAME_KEY, auth.fullName);
    this.tokenSignal.set(auth.token);
    this.nameSignal.set(auth.fullName);
  }

  end(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(NAME_KEY);
    this.tokenSignal.set(null);
    this.nameSignal.set(null);
  }
}

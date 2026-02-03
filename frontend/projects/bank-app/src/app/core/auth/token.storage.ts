import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenStorage {
  private key = 'token';

  get(): string | null {
    return localStorage.getItem(this.key);
  }

  set(token: string) {
    localStorage.setItem(this.key, token);
  }

  clear() {
    localStorage.removeItem(this.key);
  }
}

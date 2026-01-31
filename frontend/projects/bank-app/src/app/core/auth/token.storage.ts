import { Injectable } from '@angular/core';

const KEY = 'bank_token';

@Injectable({ providedIn: 'root' })
export class TokenStorage {
  get(): string | null {
    return localStorage.getItem(KEY);
  }
  set(token: string) {
    localStorage.setItem(KEY, token);
  }
  clear() {
    localStorage.removeItem(KEY);
  }
}

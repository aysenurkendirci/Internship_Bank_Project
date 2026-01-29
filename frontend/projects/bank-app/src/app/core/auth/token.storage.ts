// src/app/core/auth/token.storage.ts
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenStorage {
  private key = 'bank_token';

  get(): string | null { return localStorage.getItem(this.key); }
  set(token: string): void { localStorage.setItem(this.key, token); }
  clear(): void { localStorage.removeItem(this.key); }
}

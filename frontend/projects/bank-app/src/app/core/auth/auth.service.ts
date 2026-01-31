import { Injectable, inject } from '@angular/core';
import { tap } from 'rxjs';

import { AuthApi } from '../../data-access/api/auth.api';
import { LoginRequest, RegisterRequest } from '../../data-access/models/auth.models';
import { TokenStorage } from './token.storage';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(AuthApi);
  private token = inject(TokenStorage);

  login(req: LoginRequest) {
    return this.api.login(req).pipe(
      tap((res: any) => {
        const token = res?.token;
        if (token) this.token.set(token);
      })
    );
  }

  register(req: RegisterRequest) {
    return this.api.register(req);
  }

  logout() {
    this.token.clear();
  }

  isLoggedIn() {
    return !!this.token.get();
  }
}

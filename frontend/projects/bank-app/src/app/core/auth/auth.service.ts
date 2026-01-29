// src/app/core/auth/auth.service.ts
import { Injectable } from '@angular/core';
import { tap } from 'rxjs';
import { AuthApi } from '../../../../../../projects/data-access/src/lib/api/auth.api';
import { LoginRequest, RegisterRequest } from '../../../../../../projects/data-access/src/lib/models/auth.models';
import { TokenStorage } from './token.storage';

@Injectable({ providedIn: 'root' })
export class AuthService {
  constructor(private api: AuthApi, private token: TokenStorage) {}

  // Login işlemi başarılı olduğunda dönen token'ı storage'a kaydeder
  login(req: LoginRequest) {
    return this.api.login(req).pipe(
      tap(res => {
        if (res && res.token) {
          this.token.set(res.token);
        }
      })
    );
  }

  // Yeni kullanıcı kaydı ve otomatik giriş
  register(req: RegisterRequest) {
    return this.api.register(req).pipe(
      tap(res => {
        if (res && res.token) {
          this.token.set(res.token);
        }
      })
    );
  }

  logout() { 
    this.token.clear(); 
    // Gerekirse sayfayı login ekranına yönlendir: this.router.navigate(['/login']);
  }
}
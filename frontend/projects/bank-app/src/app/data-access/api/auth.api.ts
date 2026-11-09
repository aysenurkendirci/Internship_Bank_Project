import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.models';
import { BaseApi } from './base-api';

/** Sign-in and sign-up. Refreshing and ending a session belong to SessionService. */
@Injectable({ providedIn: 'root' })
export class AuthApi extends BaseApi {
  protected readonly root = '/api/auth';

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(this.url('/login'), req);
  }

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(this.url('/register'), req);
  }
}

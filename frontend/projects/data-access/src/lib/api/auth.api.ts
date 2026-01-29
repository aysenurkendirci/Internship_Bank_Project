// projects/data-access/src/lib/api/auth.api.ts
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.models';
import { ENDPOINTS } from './endpoints';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  constructor(private http: HttpClient) {}

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(ENDPOINTS.auth.login, req);
  }

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(ENDPOINTS.auth.register, req);
  }
}

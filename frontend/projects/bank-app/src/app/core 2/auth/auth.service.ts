import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);

  private baseUrl = 'http://localhost:5164/api/auth';

  login(payload: { email: string; password: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/login`, payload);
  }

  register(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, payload);
  }
}

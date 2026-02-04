import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../core/api/api-base-url.token';
import { Observable } from 'rxjs';

export type AccountDetailResponse = {
  accountId: number;
  type: string;
  iban: string;
  balance: number;
  status: string;
  createdAt: string;
  ownerFirstName: string;
  membership: string;
};

@Injectable({ providedIn: 'root' })
export class AccountsApi {
  private http = inject(HttpClient);
  private base = inject(API_BASE_URL);

  getById(id: number): Observable<AccountDetailResponse> {
    return this.http.get<AccountDetailResponse>(`${this.base}/api/accounts/${id}`);
  }
}

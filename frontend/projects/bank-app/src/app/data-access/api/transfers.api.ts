import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api/api-base-url.token';

export type CreateTransferRequest = {
  fromAccountId: number;
  toAccountId: number | null;
  toCardId: number | null;
  amount: number;
  note: string | null;
};


export type TransferResponse = { status: string };

@Injectable({ providedIn: 'root' })
export class TransfersApi {
  private http = inject(HttpClient);
  private base = inject(API_BASE_URL);

  create(req: CreateTransferRequest): Observable<TransferResponse> {
    return this.http.post<TransferResponse>(`${this.base}/api/transfers`, req);
  }
}

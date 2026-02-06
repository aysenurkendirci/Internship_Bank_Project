import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api/api-base-url.token';

export type TransactionItem = {
  txId: number;
  title: string;
  category: string;
  amount: number;
  direction: 'IN' | 'OUT';
  createdAt: string;
};

@Injectable({ providedIn: 'root' })
export class TransactionsApi {
  private http = inject(HttpClient);
  private base = inject(API_BASE_URL);

  recentByAccount(accountId: number): Observable<TransactionItem[]> {
    return this.http.get<TransactionItem[]>(
      `${this.base}/api/transactions/recent?accountId=${accountId}`
    );
  }

  recentByCard(cardId: number): Observable<TransactionItem[]> {
    return this.http.get<TransactionItem[]>(
      `${this.base}/api/transactions/recent?cardId=${cardId}`
    );
  }
}

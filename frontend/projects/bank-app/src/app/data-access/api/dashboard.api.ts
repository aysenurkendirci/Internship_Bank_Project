import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../core/api/api-base-url.token';
import { Observable } from 'rxjs';

export type DashboardResponse = {
  user: { userId: number; firstName: string; membership: string };
  totalWealth: number;
  wealthChangeRate: number;
  cards: Array<{
    cardId: number;
    cardNoMasked: string;
    cardType: string;
    isVirtual: boolean;
    status: string;
    balance: number;
    settings?: { contactless: boolean; onlineUse: boolean };
    limits?: { dailyLimit: number; monthlyLimit: number };
  }>;
  recentTransactions: Array<{
    txId: number;
    title: string;
    category: string;
    amount: number;
    direction: string;
    createdAt: string;
  }>;
};

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private http = inject(HttpClient);
  private baseUrl = inject(API_BASE_URL);

  getDashboard(): Observable<DashboardResponse> {
    return this.http.get<DashboardResponse>(`${this.baseUrl}/api/dashboard`);
  }
}

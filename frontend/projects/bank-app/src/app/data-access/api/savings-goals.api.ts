import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api/api-base-url.token';

export type SavingsGoalItem = {
  goalId: number;
  title: string;
  targetAmount: number;
  currentAmount: number;
  progressPercent: number;
  status: string;
  createdAt: string;
};

export type CreateSavingsGoalRequest = {
  title: string;
  targetAmount: number;
};

@Injectable({ providedIn: 'root' })
export class SavingsGoalsApi {
  private http = inject(HttpClient);
  private base = inject(API_BASE_URL);

  list(): Observable<SavingsGoalItem[]> {
    return this.http.get<SavingsGoalItem[]>(`${this.base}/api/dashboard/savings-goals`);
  }

  create(req: CreateSavingsGoalRequest): Observable<any> {
    return this.http.post(`${this.base}/api/dashboard/savings-goals`, req);
  }

  addContribution(goalId: number, amount: number): Observable<any> {
    return this.http.post(`${this.base}/api/dashboard/savings-goals/${goalId}/contributions`, { amount });
  }

  delete(goalId: number): Observable<any> {
    return this.http.delete(`${this.base}/api/dashboard/savings-goals/${goalId}`);
  }
}

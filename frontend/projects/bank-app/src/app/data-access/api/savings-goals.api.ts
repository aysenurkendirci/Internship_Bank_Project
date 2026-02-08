import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api/api-base-url.token';

export interface SavingsGoalResponse {
  goalId: number;
  title: string;
  targetAmount: number;
  currentAmount: number;
  progressPercent: number;
  status: string;
  createdAt: string;
}

export interface CreateSavingsGoalRequest {
  title: string;
  targetAmount: number;
}

@Injectable({ providedIn: 'root' })
export class SavingsGoalsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getMyGoals(): Observable<readonly SavingsGoalResponse[]> {
    return this.http.get<readonly SavingsGoalResponse[]>(
      `${this.baseUrl}/api/dashboard/savings-goals`
    );
  }

  createGoal(req: CreateSavingsGoalRequest): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/api/dashboard/savings-goals`,
      req
    );
  }
}

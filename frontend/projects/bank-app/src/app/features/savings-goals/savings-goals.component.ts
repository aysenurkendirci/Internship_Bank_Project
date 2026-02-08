import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BehaviorSubject, EMPTY, Observable, combineLatest } from 'rxjs';
import { catchError, map, shareReplay, startWith, switchMap, tap } from 'rxjs/operators';

import {
  SavingsGoalsApi,
  SavingsGoalResponse,
  CreateSavingsGoalRequest,
} from '../../data-access/api/savings-goals.api';

import { DashboardApi } from '../../data-access/api/dashboard.api';

@Component({
  selector: 'app-savings-goals',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './savings-goals.component.html',
})
export class SavingsGoalsComponent {
  private readonly goalsApi = inject(SavingsGoalsApi);
  private readonly dashboardApi = inject(DashboardApi);
  private readonly fb = inject(FormBuilder);

  // ✅ modal state
  isCreateOpen = false;
  isSaving = false;

  // ✅ form
  form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(2)]],
    targetAmount: [null as number | null, [Validators.required, Validators.min(1)]],
  });

  // ✅ refresh trigger
  private readonly refresh$ = new BehaviorSubject<void>(undefined);

  // ✅ goals list (refreshable)
  goals$: Observable<readonly SavingsGoalResponse[]> = this.refresh$.pipe(
    switchMap(() =>
      this.goalsApi.getMyGoals().pipe(
        catchError((err) => {
          console.error('getMyGoals error:', err);
          return EMPTY; // UI boş kalsın, crash olmasın
        })
      )
    ),
    shareReplay(1)
  );

  // ✅ Dashboard total wealth -> "Toplam Birikim"
  // (Dashboard endpoint'in totalWealth dönüyor varsayımı)
  totalSaved$: Observable<number> = this.dashboardApi.getDashboard().pipe(
    map((d) => d?.totalWealth ?? 0),
    catchError((err) => {
      console.error('getDashboard error:', err);
      // fallback: goals üzerinden topla
      return this.goals$.pipe(
        map((goals) => goals.reduce((sum, g) => sum + (g.currentAmount ?? 0), 0))
      );
    }),
    shareReplay(1)
  );

  activeGoalsCount$ = this.goals$.pipe(
    map(
      (goals) =>
        goals.filter((g) => (g.status ?? '').toUpperCase() !== 'COMPLETED').length
    ),
    shareReplay(1)
  );

  // ✅ UI helpers
  openCreate(): void {
    this.isCreateOpen = true;
    this.form.reset({ title: '', targetAmount: null });
  }

  closeCreate(): void {
    this.isCreateOpen = false;
  }

  submitCreate(): void {
    if (this.form.invalid || this.isSaving) return;

    const title = (this.form.value.title ?? '').trim();
    const targetAmount = Number(this.form.value.targetAmount);

    const req: CreateSavingsGoalRequest = { title, targetAmount };

    this.isSaving = true;

    this.goalsApi.createGoal(req).pipe(
      tap(() => {
        // ✅ başarı -> modal kapat + liste refresh
        this.closeCreate();
        this.refresh$.next();
      }),
      catchError((err) => {
        console.error('createGoal error:', err);
        alert('Hedef oluşturulamadı. Backend loglarını kontrol et.');
        return EMPTY;
      }),
      tap(() => (this.isSaving = false))
    ).subscribe();
  }

  progressPercent(goal: SavingsGoalResponse): number {
    return Number(goal.progressPercent ?? 0);
  }

  remainingAmount(goal: SavingsGoalResponse): number {
    const remaining = (goal.targetAmount ?? 0) - (goal.currentAmount ?? 0);
    return remaining > 0 ? remaining : 0;
  }

  formatTRY(value: number): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
      maximumFractionDigits: 2,
    }).format(value ?? 0);
  }
}

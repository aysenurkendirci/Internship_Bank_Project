import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BehaviorSubject, combineLatest, map, switchMap } from 'rxjs';

import { SavingsGoalsApi, SavingsGoalItem } from '../../data-access/api/savings-goals.api';

@Component({
  selector: 'app-savings-goals',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './savings-goals.component.html',
})
export class SavingsGoalsComponent {
  private api = inject(SavingsGoalsApi);
  private fb = inject(FormBuilder);

  private refresh$ = new BehaviorSubject<void>(undefined);

  goals$ = this.refresh$.pipe(
    switchMap(() => this.api.list())
  );

  totalSaved$ = this.goals$.pipe(
    map(goals => goals.reduce((sum, g) => sum + (g.currentAmount ?? 0), 0))
  );

  activeGoalsCount$ = this.goals$.pipe(
    map(goals => goals.filter(g => (g.status ?? 'ACTIVE') !== 'DELETED').length)
  );

  // create modal
  isCreateOpen = false;
  isSaving = false;

  form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(2)]],
    targetAmount: [null as number | null, [Validators.required, Validators.min(1)]],
  });

  // contribute modal
  isContributeOpen = false;
  isContributing = false;
  selectedGoal: SavingsGoalItem | null = null;

  contributeForm = this.fb.group({
    amount: [null as number | null, [Validators.required, Validators.min(1)]],
  });

  openCreate() {
    this.form.reset();
    this.isCreateOpen = true;
  }

  closeCreate() {
    this.isCreateOpen = false;
  }

  submitCreate() {
    if (this.form.invalid) return;

    this.isSaving = true;
    const v = this.form.value;

    this.api.create({
      title: v.title!,
      targetAmount: Number(v.targetAmount),
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.isCreateOpen = false;
        this.refresh();
      },
      error: (e) => {
        console.error(e);
        this.isSaving = false;
        alert('Hedef oluşturulamadı.');
      },
    });
  }

  // ✅ contribute
  openContribute(goal: SavingsGoalItem) {
    this.selectedGoal = goal;
    this.contributeForm.reset();
    this.isContributeOpen = true;
  }

  closeContribute() {
    this.isContributeOpen = false;
    this.selectedGoal = null;
  }

  submitContribute() {
    if (!this.selectedGoal) return;
    if (this.contributeForm.invalid) return;

    const amount = Number(this.contributeForm.value.amount);
    this.isContributing = true;

    this.api.addContribution(this.selectedGoal.goalId, amount).subscribe({
      next: () => {
        this.isContributing = false;
        this.isContributeOpen = false;
        this.selectedGoal = null;
        this.refresh();
      },
      error: (e) => {
        console.error(e);
        this.isContributing = false;
        alert('Para eklenemedi.');
      },
    });
  }

  // ✅ delete goal
  deleteGoal(goal: SavingsGoalItem) {
    const ok = confirm(`"${goal.title}" hedefini silmek istiyor musun?`);
    if (!ok) return;

    this.api.delete(goal.goalId).subscribe({
      next: () => this.refresh(),
      error: (e) => {
        console.error(e);
        alert('Hedef silinemedi.');
      },
    });
  }

  refresh() {
    this.refresh$.next();
  }

  // helpers (HTML zaten bunları çağırıyor)
  formatTRY(value: number) {
    try {
      return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(value ?? 0);
    } catch {
      return `₺${(value ?? 0).toFixed(2)}`;
    }
  }

  remainingAmount(goal: SavingsGoalItem) {
    const target = goal.targetAmount ?? 0;
    const current = goal.currentAmount ?? 0;
    return Math.max(target - current, 0);
  }

  progressPercent(goal: SavingsGoalItem) {
    const p = goal.progressPercent ?? 0;
    return Math.max(0, Math.min(100, Number(p)));
  }
}

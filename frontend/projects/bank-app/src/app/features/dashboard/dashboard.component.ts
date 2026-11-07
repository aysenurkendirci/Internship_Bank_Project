import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { Component, inject, signal } from '@angular/core';

import { problemMessage } from '../../core/http/problem-details';
import { DashboardApi } from '../../data-access/api/dashboard.api';
import { DashboardResponse } from '../../data-access/models/dashboard.models';
import { ACCOUNT_STATUS_LABELS, ACCOUNT_TYPE_DISPLAY } from './account-display';

@Component({
  selector: 'app-dashboard-page',
  imports: [CurrencyPipe, DatePipe, NgClass],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  private readonly api = inject(DashboardApi);

  protected readonly data = signal<DashboardResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly accountTypeDisplay = ACCOUNT_TYPE_DISPLAY;
  protected readonly accountStatusLabels = ACCOUNT_STATUS_LABELS;
  protected readonly cardBackgrounds = ['bg-slate-900', 'bg-blue-600', 'bg-indigo-500'];

  constructor() {
    this.api.getDashboard().subscribe({
      next: (response) => {
        this.data.set(response);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(problemMessage(err, 'Panel verileri yüklenemedi.'));
        this.loading.set(false);
      },
    });
  }
}

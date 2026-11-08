import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { problemMessage } from '../../core/http/problem-details';
import { DashboardApi } from '../../data-access/api/dashboard.api';
import { PlanningApi } from '../../data-access/api/finance.api';
import { DashboardResponse } from '../../data-access/models/dashboard.models';
import { Projection } from '../../data-access/models/finance.models';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { ProjectionSummaryComponent } from '../cash-flow/components/projection-summary.component';
import { AccountListComponent } from './components/account-list.component';
import { BankCardTileComponent } from './components/bank-card-tile.component';
import { RecentTransactionsComponent } from './components/recent-transactions.component';

@Component({
  selector: 'app-dashboard-page',
  imports: [
    CurrencyPipe,
    RouterLink,
    NoticeComponent,
    ProjectionSummaryComponent,
    BankCardTileComponent,
    RecentTransactionsComponent,
    AccountListComponent,
  ],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  private readonly api = inject(DashboardApi);
  private readonly planning = inject(PlanningApi);

  protected readonly data = signal<DashboardResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  /** The next three months at a glance; loaded on its own so a failure here never hides the accounts. */
  protected readonly outlook = signal<Projection | null>(null);

  constructor() {
    this.planning.projection(3).subscribe({ next: (projection) => this.outlook.set(projection) });

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

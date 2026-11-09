import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { BasePage } from '../../core/page/base-page';
import { BalancePrivacyService } from '../../core/ui/balance-privacy.service';
import { DashboardApi } from '../../data-access/api/dashboard.api';
import { PlanningApi } from '../../data-access/api/finance.api';
import { DashboardResponse } from '../../data-access/models/dashboard.models';
import { Projection } from '../../data-access/models/finance.models';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { BankCardTileComponent } from '../../shared/finance/bank-card-tile.component';
import { MoneyComponent } from '../../shared/finance/money.component';
import { ProjectionSummaryComponent } from '../../shared/finance/projection-summary.component';
import { SkeletonComponent } from '../../shared/ui/skeleton.component';
import { AccountListComponent } from './components/account-list.component';
import { RecentTransactionsComponent } from './components/recent-transactions.component';

function greetingFor(hour: number): string {
  if (hour < 6) return 'İyi geceler';
  if (hour < 12) return 'Günaydın';
  if (hour < 18) return 'İyi günler';
  return 'İyi akşamlar';
}

/** Container of the home screen: fetches the data and hands it to the display components below. */
@Component({
  selector: 'app-dashboard-page',
  imports: [
    DatePipe,
    RouterLink,
    MoneyComponent,
    NoticeComponent,
    SkeletonComponent,
    ProjectionSummaryComponent,
    BankCardTileComponent,
    RecentTransactionsComponent,
    AccountListComponent,
  ],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent extends BasePage {
  private readonly api = inject(DashboardApi);
  private readonly planning = inject(PlanningApi);

  protected readonly privacy = inject(BalancePrivacyService);
  protected readonly today = new Date();
  protected readonly greeting = greetingFor(this.today.getHours());

  protected readonly data = signal<DashboardResponse | null>(null);

  /** The next three months at a glance; loaded on its own so a failure here never hides the accounts. */
  protected readonly outlook = signal<Projection | null>(null);

  constructor() {
    super();
    this.planning.projection(3).subscribe({ next: (projection) => this.outlook.set(projection) });
    this.load(this.api.getDashboard(), (response) => this.data.set(response), 'Panel verileri yüklenemedi.');
  }
}

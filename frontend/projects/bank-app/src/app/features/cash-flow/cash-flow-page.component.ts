import { NgClass } from '@angular/common';
import { Component, inject, signal } from '@angular/core';

import { BasePage } from '../../core/page/base-page';
import { PlanningApi } from '../../data-access/api/finance.api';
import { Projection } from '../../data-access/models/finance.models';
import { ProjectionSummaryComponent } from '../../shared/finance/projection-summary.component';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { BalanceChartComponent } from './components/balance-chart.component';
import { MonthlyFlowChartComponent } from './components/monthly-flow-chart.component';
import { MonthlySummaryTableComponent } from './components/monthly-summary-table.component';
import { ProjectionMovementsComponent } from './components/projection-movements.component';

/** Container: loads the projection for the chosen horizon and hands it to the display components. */
@Component({
  selector: 'app-cash-flow-page',
  imports: [
    NgClass,
    PageHeaderComponent,
    NoticeComponent,
    ProjectionSummaryComponent,
    BalanceChartComponent,
    MonthlyFlowChartComponent,
    MonthlySummaryTableComponent,
    ProjectionMovementsComponent,
  ],
  templateUrl: './cash-flow-page.component.html',
})
export class CashFlowPageComponent extends BasePage {
  private readonly api = inject(PlanningApi);

  protected readonly horizons = [3, 6, 12];
  protected readonly months = signal(6);
  protected readonly projection = signal<Projection | null>(null);

  constructor() {
    super();
    this.fetch(this.months());
  }

  protected selectHorizon(months: number): void {
    this.months.set(months);
    this.fetch(months);
  }

  private fetch(months: number): void {
    this.load(this.api.projection(months), (projection) => this.projection.set(projection), 'Projeksiyon hesaplanamadı.');
  }
}

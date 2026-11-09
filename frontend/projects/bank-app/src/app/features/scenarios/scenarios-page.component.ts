import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';

import { BasePage } from '../../core/page/base-page';
import { CreditCardsApi, PlanningApi } from '../../data-access/api/finance.api';
import { CreditCard, InstallmentScenarioRequest, ScenarioComparison } from '../../data-access/models/finance.models';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { toComparisonRows } from './comparison-rows';
import { ScenarioChartComponent } from './components/scenario-chart.component';
import { ScenarioComparisonTableComponent } from './components/scenario-comparison-table.component';
import { ScenarioFormComponent } from './components/scenario-form.component';

/** Container: loads the cards, sends the scenario and hands the result to the display components. */
@Component({
  selector: 'app-scenarios-page',
  imports: [
    DatePipe,
    PageHeaderComponent,
    NoticeComponent,
    EmptyStateComponent,
    ScenarioFormComponent,
    ScenarioComparisonTableComponent,
    ScenarioChartComponent,
  ],
  templateUrl: './scenarios-page.component.html',
})
export class ScenariosPageComponent extends BasePage {
  private readonly planning = inject(PlanningApi);
  private readonly cardsApi = inject(CreditCardsApi);

  protected readonly cards = signal<CreditCard[]>([]);
  protected readonly comparison = signal<ScenarioComparison | null>(null);

  protected readonly rows = computed(() => {
    const result = this.comparison();
    return result ? toComparisonRows(result) : [];
  });

  constructor() {
    super();
    // Scenarios run in the reporting currency, so only TRY cards are offered.
    this.load(this.cardsApi.list(), (cards) => this.cards.set(cards.filter((c) => c.currencyCode === 'TRY')), 'Kartlar yüklenemedi.');
  }

  protected compare(request: InstallmentScenarioRequest): void {
    this.act(this.planning.compareInstallments(request), {
      fallback: 'Karşılaştırma yapılamadı.',
      onDone: (result) => this.comparison.set(result),
    });
  }
}

import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { SpendingAnomaly } from '../../../data-access/models/finance.models';
import { EmptyStateComponent } from '../../../shared/ui/empty-state.component';

/** Unusually large purchases, each explained against what is typical for the user. Not a judgement. */
@Component({
  selector: 'app-anomalies',
  imports: [EmptyStateComponent, CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (anomalies().length === 0) {
      <app-empty-state>Son aylarda alışkanlıklarınızdan belirgin şekilde ayrılan bir harcama yok.</app-empty-state>
    }

    <ul class="divide-y divide-line">
      @for (anomaly of anomalies(); track $index) {
        <li class="py-3 flex flex-wrap items-center justify-between gap-3">
          <div>
            <p class="font-semibold text-ink">
              {{ anomaly.description }}
              <span class="text-sm font-semibold text-ink-faint">· {{ anomaly.date | date: 'd MMM y' }}</span>
            </p>
            <p class="text-sm font-semibold text-ink-soft">
              {{ anomaly.comparedWith }} için tipik tutarın ({{ anomaly.typicalAmount | currency }}) {{ anomaly.timesTypical }} katı
            </p>
          </div>
          <span class="font-semibold text-ink">{{ anomaly.amount | currency }}</span>
        </li>
      }
    </ul>
  `,
})
export class AnomaliesComponent {
  readonly anomalies = input.required<readonly SpendingAnomaly[]>();
}

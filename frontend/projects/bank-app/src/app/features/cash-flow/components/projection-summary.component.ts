import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { Projection } from '../../../data-access/models/finance.models';
import { StatTileComponent } from '../../../shared/ui/stat-tile.component';

/** The three headline figures of a projection. */
@Component({
  selector: 'app-projection-summary',
  imports: [StatTileComponent],
  providers: [CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'grid gap-4 sm:grid-cols-3' },
  template: `
    <app-stat-tile label="Başlangıç bakiyesi" [value]="money(projection().openingBalance)" hint="Aktif TL hesaplarınız" />
    <app-stat-tile
      label="Dönemin en düşük bakiyesi"
      [value]="money(lowest()?.lowestBalance ?? projection().openingBalance)"
      [hint]="lowestDate()"
      [critical]="(lowest()?.lowestBalance ?? 0) < 0"
    />
    <app-stat-tile
      label="Bakiyenin eksiye düştüğü ay"
      [value]="monthsBelowZero() + ' / ' + projection().months.length"
      hint="Ay sonu artıda olsa bile ay içinde"
      [critical]="monthsBelowZero() > 0"
    />
  `,
})
export class ProjectionSummaryComponent {
  readonly projection = input.required<Projection>();

  private readonly currency = inject(CurrencyPipe);
  private readonly date = inject(DatePipe);

  protected readonly lowest = computed(() => {
    const months = this.projection().months;
    return months.length === 0 ? null : months.reduce((low, m) => (m.lowestBalance < low.lowestBalance ? m : low));
  });

  protected readonly lowestDate = computed(() => {
    const date = this.lowest()?.lowestBalanceDate;
    return date ? this.date.transform(date, 'd MMMM y') : null;
  });

  protected readonly monthsBelowZero = computed(() => this.projection().months.filter((m) => m.balanceGoesNegative).length);

  protected money(value: number): string {
    return this.currency.transform(value) ?? '';
  }
}

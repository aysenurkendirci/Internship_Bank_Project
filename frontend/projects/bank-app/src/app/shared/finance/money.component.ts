import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { BalancePrivacyService } from '../../core/ui/balance-privacy.service';

/** A balance that respects the "hide my balances" choice. */
@Component({
  selector: 'app-money',
  imports: [CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `{{ privacy.hidden() ? '••••••' : (amount() | currency: currency() : 'symbol-narrow') }}`,
})
export class MoneyComponent {
  readonly amount = input.required<number>();
  readonly currency = input('TRY');

  protected readonly privacy = inject(BalancePrivacyService);
}

import { Injectable, signal } from '@angular/core';

const KEY = 'balancesHidden';

/** The "hide my balances" choice, remembered per browser for people who open the app in public. */
@Injectable({ providedIn: 'root' })
export class BalancePrivacyService {
  private readonly hiddenSignal = signal(localStorage.getItem(KEY) === '1');

  readonly hidden = this.hiddenSignal.asReadonly();

  toggle(): void {
    const hidden = !this.hiddenSignal();
    this.hiddenSignal.set(hidden);
    localStorage.setItem(KEY, hidden ? '1' : '0');
  }
}

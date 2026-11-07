import { registerLocaleData } from '@angular/common';
import localeTr from '@angular/common/locales/tr';
import { DEFAULT_CURRENCY_CODE, LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { RecurringPayment } from '../../../data-access/models/finance.models';
import { RecurringPaymentsComponent } from './recurring-payments.component';

registerLocaleData(localeTr);

const payment = (merchant: string, isTracked: boolean): RecurringPayment => ({
  merchant,
  direction: 'Outbound',
  pattern: 'Fixed',
  typicalAmount: 229.99,
  typicalDay: 5,
  monthsObserved: 12,
  lastSeen: '2026-03-05',
  nextExpected: '2026-04-05',
  category: 'Abonelik',
  isTracked,
});

describe('RecurringPaymentsComponent', () => {
  function render(payments: RecurringPayment[]) {
    TestBed.configureTestingModule({
      providers: [
        { provide: LOCALE_ID, useValue: 'tr' },
        { provide: DEFAULT_CURRENCY_CODE, useValue: 'TRY' },
      ],
    });
    const fixture = TestBed.createComponent(RecurringPaymentsComponent);
    fixture.componentRef.setInput('payments', payments);
    fixture.detectChanges();
    return fixture;
  }

  it('offers tracking only for payments that are not tracked yet', () => {
    const element = render([payment('NETFLIX.COM', true), payment('SPOTIFY', false)]).nativeElement as HTMLElement;

    const buttons = element.querySelectorAll('button');
    expect(buttons.length).toBe(1);
    expect(element.textContent).toContain('Takipte');
  });

  it('emits the payment the user chose to track', () => {
    const fixture = render([payment('SPOTIFY', false)]);
    const emitted: RecurringPayment[] = [];
    fixture.componentInstance.track.subscribe((p) => emitted.push(p));

    (fixture.nativeElement as HTMLElement).querySelector('button')!.click();

    expect(emitted.map((p) => p.merchant)).toEqual(['SPOTIFY']);
  });
});

import { TestBed } from '@angular/core/testing';

import { CreditCard, InstallmentScenarioRequest } from '../../../data-access/models/finance.models';
import { ScenarioFormComponent } from './scenario-form.component';

const card: CreditCard = {
  id: 5,
  name: 'Bonus',
  limitAmount: 50000,
  currencyCode: 'TRY',
  statementDay: 20,
  paymentDueDays: 10,
  version: 'AAAAAAAAB9Q=',
};

describe('ScenarioFormComponent', () => {
  function render() {
    const fixture = TestBed.createComponent(ScenarioFormComponent);
    fixture.componentRef.setInput('cards', [card]);
    fixture.detectChanges();

    const emitted: InstallmentScenarioRequest[] = [];
    fixture.componentInstance.compare.subscribe((request) => emitted.push(request));

    const element = fixture.nativeElement as HTMLElement;
    const type = (selector: string, value: string, index = 0) => {
      const input = element.querySelectorAll<HTMLInputElement>(selector)[index];
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    const submit = () => {
      element.querySelector('form')!.dispatchEvent(new Event('submit'));
      fixture.detectChanges();
    };

    return { element, emitted, type, submit };
  }

  it('does not compare options whose price is missing', () => {
    const { emitted, type, submit } = render();
    type('input[formControlName="description"]', 'Laptop');

    submit();

    expect(emitted).toEqual([]);
  });

  it('emits the purchase with every option and the first card preselected', () => {
    const { emitted, type, submit } = render();
    type('input[formControlName="description"]', 'Laptop');
    type('input[formControlName="totalAmount"]', '9000', 0);
    type('input[formControlName="totalAmount"]', '9450', 1);

    submit();

    expect(emitted).toHaveLength(1);
    expect(emitted[0]).toMatchObject({
      creditCardId: 5,
      description: 'Laptop',
      months: 3,
      options: [
        { installmentCount: 3, totalAmount: 9000 },
        { installmentCount: 6, totalAmount: 9450 },
      ],
    });
  });
});

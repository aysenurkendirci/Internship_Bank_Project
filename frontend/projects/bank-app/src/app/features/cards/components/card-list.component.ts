import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { CreditCard } from '../../../data-access/models/finance.models';

@Component({
  selector: 'app-card-list',
  imports: [CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="grid gap-4 md:grid-cols-2">
      @for (card of cards(); track card.id) {
        <li class="panel flex flex-col gap-3">
          <div class="flex items-start justify-between">
            <div>
              <p class="text-lg font-black text-slate-900">{{ card.name }}</p>
              <p class="text-sm font-semibold text-slate-500">
                Kesim günü: {{ card.statementDay }} · Son ödeme: kesimden {{ card.paymentDueDays }} gün sonra
              </p>
            </div>
            <p class="font-black text-slate-900">{{ card.limitAmount | currency: card.currencyCode : 'symbol-narrow' }}</p>
          </div>
          <div class="flex gap-4">
            <button type="button" class="btn-link" (click)="edit.emit(card)">Düzenle</button>
            <button type="button" class="btn-link text-red-600" (click)="remove.emit(card)" [disabled]="busy()">Sil</button>
          </div>
        </li>
      }
    </ul>
  `,
})
export class CardListComponent {
  readonly cards = input.required<readonly CreditCard[]>();
  readonly busy = input(false);
  readonly edit = output<CreditCard>();
  readonly remove = output<CreditCard>();
}

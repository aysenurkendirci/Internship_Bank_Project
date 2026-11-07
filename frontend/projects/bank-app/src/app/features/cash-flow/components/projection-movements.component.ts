import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';

import { ProjectionEntry, ProjectionEntryKind } from '../../../data-access/models/finance.models';

const KIND_LABELS: Record<ProjectionEntryKind, string> = {
  Income: 'Gelir',
  Expense: 'Gider',
  Subscription: 'Abonelik',
  CardPayment: 'Kart ekstresi',
};

/** Every projected movement; a card payment expands to the statement lines behind it. */
@Component({
  selector: 'app-projection-movements',
  imports: [CurrencyPipe, DatePipe, NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    @if (entries().length === 0) {
      <p class="text-slate-500 font-semibold">Bu dönemde hareket yok. Gelir, gider veya kart ekleyerek başlayın.</p>
    }
    <ul class="divide-y divide-slate-100">
      @for (entry of entries(); track $index) {
        <li class="py-3">
          <div class="flex flex-wrap items-center justify-between gap-2">
            <div class="flex items-center gap-3">
              <span class="text-sm font-bold text-slate-400 w-24">{{ entry.date | date: 'd MMM y' }}</span>
              <span class="font-bold text-slate-800">{{ entry.description }}</span>
              <span class="badge bg-slate-100 text-slate-600">{{ kindLabels[entry.kind] }}</span>
            </div>
            <div class="flex items-center gap-3">
              @if (entry.charges.length > 0) {
                <button type="button" class="btn-link" (click)="toggle($index)" [attr.aria-expanded]="open() === $index">
                  {{ entry.charges.length }} kalem
                </button>
              }
              <span class="font-black" [ngClass]="entry.amount < 0 ? 'text-slate-900' : 'text-emerald-600'">
                {{ entry.amount | currency }}
              </span>
            </div>
          </div>
          @if (open() === $index) {
            <ul class="mt-2 ml-28 text-sm text-slate-600 space-y-1">
              @if (entry.statementDate) {
                <li class="text-xs text-slate-400 font-bold">Ekstre kesimi: {{ entry.statementDate | date: 'd MMMM y' }}</li>
              }
              @for (charge of entry.charges; track $index) {
                <li class="flex justify-between max-w-md"><span>{{ charge.description }}</span><span>{{ charge.amount | currency }}</span></li>
              }
            </ul>
          }
        </li>
      }
    </ul>
  `,
})
export class ProjectionMovementsComponent {
  readonly entries = input.required<readonly ProjectionEntry[]>();

  protected readonly kindLabels = KIND_LABELS;
  protected readonly open = signal<number | null>(null);

  protected toggle(index: number): void {
    this.open.update((current) => (current === index ? null : index));
  }
}

import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { CardItem } from '../../../data-access/models/dashboard.models';

const STATUS_LABELS: Record<string, string> = {
  Blocked: 'Geçici kapalı',
  Cancelled: 'İptal',
};

/** One issued card. Only a status other than active is mentioned: an active card is the normal case. */
@Component({
  selector: 'app-bank-card-tile',
  imports: [CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'w-72 h-40 shrink-0 rounded-xl p-4 text-white flex flex-col justify-between',
    '[class.bg-brand]': '!card().isVirtual',
    '[class.bg-brand-muted]': 'card().isVirtual',
    '[class.opacity-60]': 'card().status !== "Active"',
  },
  template: `
    <div class="flex justify-between text-sm">
      <span class="font-medium">{{ card().cardType === 'Credit' ? 'Kredi kartı' : 'Banka kartı' }}</span>
      <span class="text-white/70">{{ statusLabel() ?? (card().isVirtual ? 'Sanal' : 'Fiziksel') }}</span>
    </div>
    <p class="tabular-numsr text-white/90">{{ card().cardNoMasked }}</p>
    <div>
      <p class="text-xs text-white/70">Bakiye</p>
      <p class="font-semibold tabular-nums">{{ card().balance | currency }}</p>
    </div>
  `,
})
export class BankCardTileComponent {
  readonly card = input.required<CardItem>();

  protected readonly statusLabel = computed(() => STATUS_LABELS[this.card().status] ?? null);
}

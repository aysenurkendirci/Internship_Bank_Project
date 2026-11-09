import { Component, inject, signal } from '@angular/core';

import { BasePage } from '../../core/page/base-page';
import { ConfirmService } from '../../core/ui/confirm.service';
import { ToastService } from '../../core/ui/toast.service';
import { BankCardsApi } from '../../data-access/api/banking.api';
import { CardSettingsRequest, CardSettingsResponse } from '../../data-access/models/banking.models';
import { CardItem } from '../../data-access/models/dashboard.models';
import { BankCardTileComponent } from '../../shared/finance/bank-card-tile.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { SkeletonComponent } from '../../shared/ui/skeleton.component';
import { CardSettingsComponent } from './components/card-settings.component';

/** The cards the bank issued: usage switches, spending limits and a temporary block. */
@Component({
  imports: [BankCardTileComponent, CardSettingsComponent, EmptyStateComponent, NoticeComponent, PageHeaderComponent, SkeletonComponent],
  template: `
    <app-page-header title="Banka kartlarım" subtitle="Kartlarınızın kullanım ayarları ve limitleri." />

    @if (error(); as message) {
      <app-notice class="mb-4" [message]="message" />
    }

    @if (loading() && cards().length === 0) {
      <app-skeleton />
    } @else if (cards().length === 0 && !error()) {
      <app-empty-state>Size tanımlı bir kart bulunmuyor.</app-empty-state>
    } @else {
      <div class="space-y-4">
        @for (card of cards(); track card.cardId) {
          <section class="panel flex flex-col md:flex-row gap-6">
            <app-bank-card-tile [card]="card" />
            <app-card-settings class="flex-1 min-w-0" [card]="card" [busy]="busy()" (save)="save(card, $event)" (toggleBlock)="toggleBlock(card)" />
          </section>
        }
      </div>
    }
  `,
})
export class BankCardsPageComponent extends BasePage {
  private readonly api = inject(BankCardsApi);
  private readonly dialog = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  protected readonly cards = signal<readonly CardItem[]>([]);

  constructor() {
    super();
    this.load(this.api.list(), (cards) => this.cards.set(cards), 'Kartlar yüklenemedi.');
  }

  protected save(card: CardItem, settings: CardSettingsRequest): void {
    this.act(this.api.updateSettings(card.cardId, settings), {
      fallback: 'Kart ayarları kaydedilemedi.',
      onDone: (updated) => {
        this.apply(updated);
        this.toast.show('Kart ayarlarınız kaydedildi.');
      },
    });
  }

  protected async toggleBlock(card: CardItem): Promise<void> {
    const block = card.status !== 'Blocked';
    if (block) {
      const yes = await this.dialog.ask({
        title: 'Kart geçici olarak kapatılsın mı?',
        message: `${card.cardNoMasked} numaralı kartla, yeniden açana kadar işlem yapılamaz.`,
        confirmLabel: 'Kapat',
        danger: true,
      });
      if (!yes) return;
    }

    this.act(this.api.setBlocked(card.cardId, block), {
      fallback: 'Kartın durumu değiştirilemedi.',
      onDone: (updated) => {
        this.apply(updated);
        this.toast.show(block ? 'Kartınız geçici olarak kapatıldı.' : 'Kartınız yeniden açıldı.');
      },
    });
  }

  /** The response carries the card's new state, so there is no need to fetch the list again. */
  private apply(updated: CardSettingsResponse): void {
    this.cards.update((cards) =>
      cards.map((card) =>
        card.cardId !== updated.id
          ? card
          : {
              ...card,
              status: updated.status,
              settings: { contactless: updated.contactless, onlineUse: updated.onlineUse },
              limits: { dailyLimit: updated.dailyLimit, monthlyLimit: updated.monthlyLimit },
            },
      ),
    );
  }
}

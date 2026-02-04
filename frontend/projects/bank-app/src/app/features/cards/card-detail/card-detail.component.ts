import { Component, inject } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-card-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './card-detail.component.html',
})
export class CardDetailComponent {
  private route = inject(ActivatedRoute);

  id = Number(this.route.snapshot.paramMap.get('id'));
  isLoading = true;

  // HTML’de card?. diye kullandığın için field şart
  card?: {
    cardId: number;
    cardNoMasked?: string;
    cardType?: string;
    isVirtual?: boolean;
    status?: string;
    balance?: number;
    settings?: { contactless: boolean; onlineUse: boolean };
    limits?: { dailyLimit: number; monthlyLimit: number };
  };

  txs: Array<{
    category: string;
    createdAt: string | Date;
    direction: 'IN' | 'OUT';
    amount: number;
  }> = [];
}

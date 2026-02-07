import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

// Servisler ve Tipler
import { CardsApi } from '../../../data-access/api/cards.api';
import { TransactionsApi, TransactionItem } from '../../../data-access/api/transactions.api';

// Bileşenler
import { TransferDrawerComponent } from '../../transfers/transfer-drawer/transfer-drawer.component';

@Component({
  selector: 'app-card-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, TransferDrawerComponent],
  templateUrl: './card-detail.component.html',
})
export class CardDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private location = inject(Location);
  private cardsApi = inject(CardsApi);
  private txApi = inject(TransactionsApi); // ✅ TransactionsApi inject edildi
  private cdr = inject(ChangeDetectorRef);

  id = 0;
  isLoading = true;
  card?: any;
  txs: TransactionItem[] = []; // ✅ Tip güvenliği için TransactionItem[] kullanıldı

  // Drawer State
  drawerOpen = false;
  drawerMode: 'between-accounts' | 'account-to-card' | 'external-iban' = 'account-to-card';

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      this.id = Number(params.get('id') ?? 0);
      console.log('[CardDetail] id:', this.id);
      if (this.id > 0) {
        this.loadCardDetails(this.id);
      }
    });
  }

  loadCardDetails(cardId: number) {
    this.isLoading = true;
    this.cardsApi.getById(cardId)
      .pipe(finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (res: any) => {
          this.card = res;
          
          // ✅ Kart verisi gelince işlemleri bağımsız olarak çekiyoruz
          this.fetchRecentTxByCard(cardId);
          
          this.cdr.detectChanges(); 
        },
        error: (err) => {
          console.error('Kart hatası:', err);
          this.isLoading = false;
        }
      });
  }

  // ✅ Kart işlemlerini getiren yeni metod
  private fetchRecentTxByCard(cardId: number) {
    this.txApi.recentByCard(cardId, 20).subscribe({
      next: (txs) => {
        this.txs = txs ?? [];
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Card tx çekme hatası:', err)
    });
  }

  openDrawer(mode: 'between-accounts' | 'account-to-card' | 'external-iban') {
    this.drawerMode = mode;
    this.drawerOpen = true;
  }

  closeDrawer() {
    this.drawerOpen = false;
  }

  onTransferSuccess() {
    // Transfer başarılı olduğunda hem kart limit/borç bilgisini hem işlemleri yeniler
    this.loadCardDetails(this.id);
  }

  goBack() { this.location.back(); }
}
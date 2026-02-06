import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CardsApi } from '../../../data-access/api/cards.api';
import { finalize } from 'rxjs';
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
  private cdr = inject(ChangeDetectorRef);

  id = 0;
  isLoading = true;
  card?: any;
  txs: any[] = [];

  // Drawer State
  drawerOpen = false;
  drawerMode: 'between-accounts' | 'account-to-card' | 'external-iban' = 'account-to-card';

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      this.id = Number(params.get('id') ?? 0);
      if (this.id > 0) this.loadCardDetails(this.id);
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
          this.txs = res?.transactions || [];
          this.cdr.detectChanges(); 
        },
        error: (err) => {
          console.error('Kart hatası:', err);
          this.isLoading = false;
        }
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
    this.loadCardDetails(this.id);
  }

  goBack() { this.location.back(); }
}
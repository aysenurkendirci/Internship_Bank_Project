import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CardsApi } from '../../../data-access/api/cards.api';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-card-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
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
        this.cdr.detectChanges(); // Veri geldiğinde HTML'i güncelle
      }))
      .subscribe({
        next: (res: any) => {
          this.card = res;
          // API'den gelen işlem listesini bağla
          this.txs = res?.transactions || [];
          console.log('Kart Detay Yüklendi:', res);
          this.cdr.detectChanges(); 
        },
        error: (err) => {
          console.error('Kart hatası:', err);
          this.isLoading = false;
        }
      });
  }

  goBack() { this.location.back(); }
}
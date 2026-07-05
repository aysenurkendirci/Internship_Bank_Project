/*import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InvestmentsApi } from '../../data-access/api/investments.api';

@Component({
  selector: 'app-investments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './investments.component.html',
})
export class InvestmentsComponent implements OnInit {
  private api = inject(InvestmentsApi);

  rates = signal<any[]>([]);
  goldRates = signal<any[]>([]);
  loading = signal(false);

  ngOnInit(): void { this.loadAll(); }

  loadAll() {
    this.loading.set(true);
    // Döviz Kurları
    this.api.getMarketRates().subscribe({
      next: (data) => {
        this.rates.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    // Altın Fiyatları
    this.api.getGoldPrices().subscribe({
      next: (data) => this.goldRates.set(data),
      error: () => {}
    });
  }

  getSaleRate(code: string): number {
    const found = this.rates().find(x => x.CurrencyCode === code);
    return Number(found?.SaleRate || 0);
  }

  getGoldSaleRate(): number {
    const found = this.goldRates().find(x => x.CurrencyCode === 'XAU' || x.ProductName?.includes('Altın'));
    return Number(found?.SaleRate || 0);
  }

  // HTML'in beklediği computed property'ler
  usdRate = computed(() => this.getSaleRate('USD'));
  eurRate = computed(() => this.getSaleRate('EUR'));
  gbpRate = computed(() => this.getSaleRate('GBP'));
  goldRate = computed(() => this.getGoldSaleRate());
}
  */
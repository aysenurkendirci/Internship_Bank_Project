import { CommonModule, DecimalPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InvestmentsApi, MarketRateItem } from '../../data-access/api/investments.api';

@Component({
  selector: 'app-investments',
  standalone: true,
  imports: [CommonModule, FormsModule, DecimalPipe, DatePipe],
  templateUrl: './investments.component.html',
})
export class InvestmentsComponent implements OnInit {
  private api = inject(InvestmentsApi);

  // --- Market Verileri ---
  rates = signal<MarketRateItem[]>([]);
  goldRates = signal<any[]>([]);
  loadingRates = signal(false);
  lastUpdated = signal<string | null>(null);
  
  // --- UI Seçimleri ---
  mode = signal<'buy' | 'sell'>('buy');
  selected = signal<string>('USD');
  tryAmount = signal<number>(0);

  // --- Hesaplayıcı (Calculator) Sinyalleri ---
  calcSourceCode = signal<string>('TRY');
  calcSourceAmount = signal<number>(0);
  calcTargetCode = signal<string>('USD');
  calcResult = signal<any>(null); // Backend'den gelen SaleAmount, SaleRate objesi
  calcLoading = signal(false);

  ngOnInit(): void {
    this.loadRates();
    this.loadGold();
  }

  loadRates() {
    this.loadingRates.set(true);
    this.api.getMarketRates().subscribe({
      next: (items) => {
        this.rates.set(items);
        this.lastUpdated.set(new Date().toLocaleTimeString());
        this.loadingRates.set(false);
      },
      error: () => this.loadingRates.set(false)
    });
  }

  loadGold() {
    this.api.getGoldPrices().subscribe(data => this.goldRates.set(data));
  }

  // --- HESAPLAYICI FONKSİYONU ---
  // Bu metod her miktar veya döviz türü değiştiğinde tetiklenir
  calculateExchange() {
    const amount = this.calcSourceAmount();
    if (amount <= 0) {
      this.calcResult.set(null);
      return;
    }

    this.calcLoading.set(true);
    const payload = {
      SourceCurrencyCode: this.calcSourceCode(),
      SourceAmount: amount,
      TargetCurrencyCode: this.calcTargetCode()
    };

    this.api.currencyCalculator(payload).subscribe({
      next: (res) => {
        // Backend'den gelen veriyi (SaleAmount vb.) sinyale kaydediyoruz
        this.calcResult.set(res);
        this.calcLoading.set(false);
      },
      error: () => {
        this.calcLoading.set(false);
        this.calcResult.set(null);
      }
    });
  }

  // --- UI SETTER METOTLARI (Otomatik Hesaplamalı) ---
  setCalcSourceAmount(v: any) { 
    this.calcSourceAmount.set(Number(v) || 0); 
    this.calculateExchange(); // Rakam girildiği an hesapla
  }
  
  setCalcSourceCode(v: any) { 
    this.calcSourceCode.set(v); 
    this.calculateExchange(); 
  }
  
  setCalcTargetCode(v: any) { 
    this.calcTargetCode.set(v); 
    this.calculateExchange(); 
  }

  swapCalc() {
    const temp = this.calcSourceCode();
    this.calcSourceCode.set(this.calcTargetCode());
    this.calcTargetCode.set(temp);
    this.calculateExchange();
  }

  // --- Kurlar ve Altın Yardımcıları ---
  getSaleRate(code: string): number {
    if (code === 'XAU') {
      const list = this.goldRates();
      return list.length > 0 ? parseFloat(list[0].SaleRate) : 0;
    }
    const found = this.rates().find(x => x.CurrencyCode === code);
    return found ? parseFloat(found.SaleRate as string) : 0;
  }

  getPurchaseRate(code: string): number {
    const found = this.rates().find(x => x.CurrencyCode === code);
    return found ? parseFloat(found.PurchaseRate as string) : 0;
  }

  appliedRateText = computed(() => {
    const rate = this.mode() === 'buy' ? this.getSaleRate(this.selected()) : this.getPurchaseRate(this.selected());
    return `${this.selected()} Kur: ${rate}`;
  });

  setMode(m: 'buy' | 'sell') { this.mode.set(m); }
  setSelected(v: any) { this.selected.set(v); }
  setTryAmount(v: any) { this.tryAmount.set(Number(v) || 0); }
  submitQuickTrade() { console.log('İşlem yapıldı'); }
}
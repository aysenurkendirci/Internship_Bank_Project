import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { Observable } from 'rxjs';

export type MarketRateItem = {
  CurrencyCode?: string;
  SaleRate?: string | number;
  PurchaseRate?: string | number;
  ProductName?: string;
};

@Injectable({ providedIn: 'root' })
export class InvestmentsApi {
  private http = inject(HttpClient);

  getMarketRates(): Observable<MarketRateItem[]> {
    return this.http.post<any>('/api/investments/market-rates', {}).pipe(
      map(res => res?.Data?.Currency || [])
    );
  }

  getGoldPrices(): Observable<any[]> {
    return this.http.post<any>('/api/investments/gold-prices', {}).pipe(
      map(res => res?.Data?.GoldRate || [])
    );
  }

  currencyCalculator(payload: any): Observable<any> {
  return this.http.post<any>('/api/investments/convert', payload).pipe(
    map(res => res?.Data?.Currency || res)
  );
}
  }

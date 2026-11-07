import { Component, WritableSignal, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';

import { problemMessage } from '../../core/http/problem-details';
import { AnalyticsApi, RecurringCashFlowsApi } from '../../data-access/api/finance.api';
import {
  CategoryTrends,
  RecurringCashFlowRequest,
  RecurringPayment,
  SpendingAnomaly,
  SpendingForecast,
} from '../../data-access/models/finance.models';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { AnomaliesComponent } from './components/anomalies.component';
import { CategoryTrendsComponent } from './components/category-trends.component';
import { RecurringPaymentsComponent } from './components/recurring-payments.component';
import { SpendingForecastComponent } from './components/spending-forecast.component';

/** Each section loads on its own: too little history to forecast must not hide the other analyses. */
interface Section<T> {
  data: T | null;
  error: string | null;
}

/** Container: loads the four analyses and handles "track this payment"; the sections only display. */
@Component({
  selector: 'app-analytics-page',
  imports: [
    PageHeaderComponent,
    NoticeComponent,
    CategoryTrendsComponent,
    RecurringPaymentsComponent,
    AnomaliesComponent,
    SpendingForecastComponent,
  ],
  templateUrl: './analytics-page.component.html',
})
export class AnalyticsPageComponent {
  private readonly api = inject(AnalyticsApi);
  private readonly recurringApi = inject(RecurringCashFlowsApi);

  protected readonly trends = signal<Section<CategoryTrends>>({ data: null, error: null });
  protected readonly recurring = signal<Section<RecurringPayment[]>>({ data: null, error: null });
  protected readonly anomalies = signal<Section<SpendingAnomaly[]>>({ data: null, error: null });
  protected readonly forecast = signal<Section<SpendingForecast>>({ data: null, error: null });
  protected readonly trackingMerchant = signal<string | null>(null);

  constructor() {
    this.load(this.api.categoryTrends(6), this.trends, 'Harcama trendleri yüklenemedi.');
    this.load(this.api.recurringPayments(), this.recurring, 'Düzenli ödemeler yüklenemedi.');
    this.load(this.api.anomalies(6), this.anomalies, 'Olağandışı harcamalar yüklenemedi.');
    this.load(this.api.forecast(3), this.forecast, 'Tahmin yapılamadı.');
  }

  /** Adds a detected payment as a recurring item, so the cash-flow projection starts counting it. */
  protected track(payment: RecurringPayment): void {
    const request: RecurringCashFlowRequest = {
      name: payment.merchant,
      kind: payment.direction === 'Inbound' ? 'Income' : payment.pattern === 'Fixed' ? 'Subscription' : 'Expense',
      category: payment.category,
      amount: payment.typicalAmount,
      currencyCode: 'TRY',
      frequency: 'Monthly',
      interval: 1,
      startDate: payment.nextExpected,
      endDate: null,
      creditCardId: null,
    };

    this.trackingMerchant.set(payment.merchant);
    this.recurringApi.create(request).subscribe({
      next: () => {
        this.trackingMerchant.set(null);
        this.recurring.update((section) => ({
          ...section,
          data: section.data?.map((p) => (p === payment ? { ...p, isTracked: true } : p)) ?? null,
        }));
      },
      error: (err) => {
        this.trackingMerchant.set(null);
        this.recurring.update((section) => ({ ...section, error: problemMessage(err, 'Kalem eklenemedi.') }));
      },
    });
  }

  private load<T>(call: Observable<T>, target: WritableSignal<Section<T>>, fallback: string): void {
    call.subscribe({
      next: (data) => target.set({ data, error: null }),
      error: (err) => target.set({ data: null, error: problemMessage(err, fallback) }),
    });
  }
}

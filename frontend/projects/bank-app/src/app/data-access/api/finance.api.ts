import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CategoryTrends,
  CreditCard,
  CreditCardRequest,
  HistoryTransaction,
  ImportResult,
  InstallmentPlan,
  InstallmentPlanRequest,
  InstallmentScenarioRequest,
  IsoDate,
  Paged,
  Projection,
  RecurringCashFlow,
  RecurringCashFlowRequest,
  RecurringPayment,
  ScenarioComparison,
  SpendingAnomaly,
  SpendingForecast,
} from '../models/finance.models';

const BASE = '/api/finance';

/** Updates must carry the version the user edited; the API answers 409 if someone changed it since. */
function ifMatch(version: string): { headers: HttpHeaders } {
  return { headers: new HttpHeaders({ 'If-Match': `"${version}"` }) };
}

@Injectable({ providedIn: 'root' })
export class CreditCardsApi {
  private readonly http = inject(HttpClient);

  list(): Observable<CreditCard[]> {
    return this.http.get<CreditCard[]>(`${BASE}/credit-cards`);
  }

  create(request: CreditCardRequest): Observable<CreditCard> {
    return this.http.post<CreditCard>(`${BASE}/credit-cards`, request);
  }

  update(id: number, request: CreditCardRequest, version: string): Observable<CreditCard> {
    return this.http.put<CreditCard>(`${BASE}/credit-cards/${id}`, request, ifMatch(version));
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${BASE}/credit-cards/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class RecurringCashFlowsApi {
  private readonly http = inject(HttpClient);

  list(): Observable<RecurringCashFlow[]> {
    return this.http.get<RecurringCashFlow[]>(`${BASE}/recurring-cash-flows`);
  }

  create(request: RecurringCashFlowRequest): Observable<RecurringCashFlow> {
    return this.http.post<RecurringCashFlow>(`${BASE}/recurring-cash-flows`, request);
  }

  update(id: number, request: RecurringCashFlowRequest, version: string): Observable<RecurringCashFlow> {
    return this.http.put<RecurringCashFlow>(`${BASE}/recurring-cash-flows/${id}`, request, ifMatch(version));
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${BASE}/recurring-cash-flows/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class InstallmentPlansApi {
  private readonly http = inject(HttpClient);

  list(): Observable<InstallmentPlan[]> {
    return this.http.get<InstallmentPlan[]>(`${BASE}/installment-plans`);
  }

  create(request: InstallmentPlanRequest): Observable<InstallmentPlan> {
    return this.http.post<InstallmentPlan>(`${BASE}/installment-plans`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${BASE}/installment-plans/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class PlanningApi {
  private readonly http = inject(HttpClient);

  projection(months: number): Observable<Projection> {
    return this.http.get<Projection>(`${BASE}/projection`, { params: { months } });
  }

  compareInstallments(request: InstallmentScenarioRequest): Observable<ScenarioComparison> {
    return this.http.post<ScenarioComparison>(`${BASE}/scenarios/installments`, request);
  }
}

@Injectable({ providedIn: 'root' })
export class TransactionsApi {
  private readonly http = inject(HttpClient);

  list(page: number, pageSize: number, from?: IsoDate, to?: IsoDate): Observable<Paged<HistoryTransaction>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<Paged<HistoryTransaction>>(`${BASE}/transactions`, { params });
  }

  importCsv(file: File): Observable<ImportResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<ImportResult>(`${BASE}/transactions/import`, form);
  }

  generateDemo(months: number, seed: number): Observable<ImportResult> {
    return this.http.post<ImportResult>(`${BASE}/transactions/demo`, null, { params: { months, seed } });
  }
}

@Injectable({ providedIn: 'root' })
export class AnalyticsApi {
  private readonly http = inject(HttpClient);

  categoryTrends(months: number): Observable<CategoryTrends> {
    return this.http.get<CategoryTrends>(`${BASE}/analytics/category-trends`, { params: { months } });
  }

  recurringPayments(): Observable<RecurringPayment[]> {
    return this.http.get<RecurringPayment[]>(`${BASE}/analytics/recurring-payments`);
  }

  anomalies(months: number): Observable<SpendingAnomaly[]> {
    return this.http.get<SpendingAnomaly[]>(`${BASE}/analytics/anomalies`, { params: { months } });
  }

  forecast(months: number): Observable<SpendingForecast> {
    return this.http.get<SpendingForecast>(`${BASE}/analytics/forecast`, { params: { months } });
  }
}

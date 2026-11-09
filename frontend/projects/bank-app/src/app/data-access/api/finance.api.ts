import { HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
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
import { BaseApi } from './base-api';

const BASE = '/api/finance';

/** Updates must carry the version the user edited; the API answers 409 if someone changed it since. */
function ifMatch(version: string): { headers: HttpHeaders } {
  return { headers: new HttpHeaders({ 'If-Match': `"${version}"` }) };
}

@Injectable({ providedIn: 'root' })
export class CreditCardsApi extends BaseApi {
  protected readonly root = `${BASE}/credit-cards`;

  list(): Observable<CreditCard[]> {
    return this.http.get<CreditCard[]>(this.url());
  }

  create(request: CreditCardRequest): Observable<CreditCard> {
    return this.http.post<CreditCard>(this.url(), request);
  }

  update(id: number, request: CreditCardRequest, version: string): Observable<CreditCard> {
    return this.http.put<CreditCard>(this.url(`/${id}`), request, ifMatch(version));
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(this.url(`/${id}`));
  }
}

@Injectable({ providedIn: 'root' })
export class RecurringCashFlowsApi extends BaseApi {
  protected readonly root = `${BASE}/recurring-cash-flows`;

  list(): Observable<RecurringCashFlow[]> {
    return this.http.get<RecurringCashFlow[]>(this.url());
  }

  create(request: RecurringCashFlowRequest): Observable<RecurringCashFlow> {
    return this.http.post<RecurringCashFlow>(this.url(), request);
  }

  update(id: number, request: RecurringCashFlowRequest, version: string): Observable<RecurringCashFlow> {
    return this.http.put<RecurringCashFlow>(this.url(`/${id}`), request, ifMatch(version));
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(this.url(`/${id}`));
  }
}

@Injectable({ providedIn: 'root' })
export class InstallmentPlansApi extends BaseApi {
  protected readonly root = `${BASE}/installment-plans`;

  list(): Observable<InstallmentPlan[]> {
    return this.http.get<InstallmentPlan[]>(this.url());
  }

  create(request: InstallmentPlanRequest): Observable<InstallmentPlan> {
    return this.http.post<InstallmentPlan>(this.url(), request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(this.url(`/${id}`));
  }
}

@Injectable({ providedIn: 'root' })
export class PlanningApi extends BaseApi {
  protected readonly root = `${BASE}`;

  projection(months: number): Observable<Projection> {
    return this.http.get<Projection>(this.url(`/projection`), { params: { months } });
  }

  compareInstallments(request: InstallmentScenarioRequest): Observable<ScenarioComparison> {
    return this.http.post<ScenarioComparison>(this.url(`/scenarios/installments`), request);
  }
}

@Injectable({ providedIn: 'root' })
export class TransactionsApi extends BaseApi {
  protected readonly root = `${BASE}/transactions`;

  list(page: number, pageSize: number, from?: IsoDate, to?: IsoDate): Observable<Paged<HistoryTransaction>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<Paged<HistoryTransaction>>(this.url(), { params });
  }

  importCsv(file: File): Observable<ImportResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<ImportResult>(this.url(`/import`), form);
  }

  generateDemo(months: number, seed: number): Observable<ImportResult> {
    return this.http.post<ImportResult>(this.url(`/demo`), null, { params: { months, seed } });
  }
}

@Injectable({ providedIn: 'root' })
export class AnalyticsApi extends BaseApi {
  protected readonly root = `${BASE}/analytics`;

  categoryTrends(months: number): Observable<CategoryTrends> {
    return this.http.get<CategoryTrends>(this.url(`/category-trends`), { params: { months } });
  }

  recurringPayments(): Observable<RecurringPayment[]> {
    return this.http.get<RecurringPayment[]>(this.url(`/recurring-payments`));
  }

  anomalies(months: number): Observable<SpendingAnomaly[]> {
    return this.http.get<SpendingAnomaly[]>(this.url(`/anomalies`), { params: { months } });
  }

  forecast(months: number): Observable<SpendingForecast> {
    return this.http.get<SpendingForecast>(this.url(`/forecast`), { params: { months } });
  }
}

import { HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Account,
  AccountCurrency,
  CardSettingsRequest,
  CardSettingsResponse,
  StatementEntry,
  TransferReceipt,
  TransferRequest,
} from '../models/banking.models';
import { CardItem } from '../models/dashboard.models';
import { Paged } from '../models/finance.models';
import { BaseApi } from './base-api';

@Injectable({ providedIn: 'root' })
export class AccountsApi extends BaseApi {
  protected readonly root = '/api/accounts';

  list(): Observable<Account[]> {
    return this.http.get<Account[]>(this.url());
  }

  open(currencyCode: AccountCurrency): Observable<Account> {
    return this.http.post<Account>(this.url(), { currencyCode });
  }

  statement(accountId: number, page: number, pageSize: number): Observable<Paged<StatementEntry>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<Paged<StatementEntry>>(this.url(`/${accountId}/statement`), { params });
  }
}

@Injectable({ providedIn: 'root' })
export class TransfersApi extends BaseApi {
  protected readonly root = '/api/transfers';

  /** The same key must be sent on every retry of one transfer; the server then runs it at most once. */
  send(request: TransferRequest, idempotencyKey: string): Observable<TransferReceipt> {
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<TransferReceipt>(this.url(), request, { headers });
  }
}

/** Cards the bank issued. The credit cards tracked for planning are CreditCardsApi. */
@Injectable({ providedIn: 'root' })
export class BankCardsApi extends BaseApi {
  protected readonly root = '/api/cards';

  list(): Observable<CardItem[]> {
    return this.http.get<CardItem[]>(this.url());
  }

  updateSettings(cardId: number, settings: CardSettingsRequest): Observable<CardSettingsResponse> {
    return this.http.put<CardSettingsResponse>(this.url(`/${cardId}/settings`), settings);
  }

  setBlocked(cardId: number, blocked: boolean): Observable<CardSettingsResponse> {
    return this.http.post<CardSettingsResponse>(this.url(`/${cardId}/${blocked ? 'block' : 'unblock'}`), null);
  }
}

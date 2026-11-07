import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { CreditCardRequest } from '../models/finance.models';
import { CreditCardsApi, TransactionsApi } from './finance.api';

describe('finance API clients', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the edited version as a quoted If-Match header on update', () => {
    const request: CreditCardRequest = { name: 'Bonus', limitAmount: 50000, currencyCode: 'TRY', statementDay: 20, paymentDueDays: 10 };

    TestBed.inject(CreditCardsApi).update(7, request, 'AAAAAAAAB9Q=').subscribe();

    const call = http.expectOne('/api/finance/credit-cards/7');
    expect(call.request.method).toBe('PUT');
    expect(call.request.headers.get('If-Match')).toBe('"AAAAAAAAB9Q="');
    expect(call.request.body).toEqual(request);
    call.flush({});
  });

  it('uploads a statement as multipart form data under the "file" field', () => {
    const file = new File(['Tarih;Açıklama;Tutar\n'], 'ekstre.csv', { type: 'text/csv' });

    TestBed.inject(TransactionsApi).importCsv(file).subscribe();

    const call = http.expectOne('/api/finance/transactions/import');
    const body = call.request.body as FormData;
    expect((body.get('file') as File).name).toBe('ekstre.csv');
    call.flush({});
  });

  it('only sends the date filters that are set', () => {
    TestBed.inject(TransactionsApi).list(2, 50, '2026-01-01').subscribe();

    const call = http.expectOne((r) => r.url === '/api/finance/transactions');
    expect(call.request.params.keys()).toEqual(['page', 'pageSize', 'from']);
    call.flush({ items: [], page: 2, pageSize: 50, totalCount: 0 });
  });
});

import { Component, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';

import { problemErrors } from '../../core/http/problem-details';
import { BasePage } from '../../core/page/base-page';
import { TransactionsApi } from '../../data-access/api/finance.api';
import { HistoryTransaction, ImportResult } from '../../data-access/models/finance.models';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { StatementUploadComponent } from './components/statement-upload.component';
import { DateRange, TransactionListComponent } from './components/transaction-list.component';

const PAGE_SIZE = 50;

@Component({
  selector: 'app-transactions-page',
  imports: [PageHeaderComponent, StatementUploadComponent, TransactionListComponent],
  template: `
    <app-page-header
      title="İşlem geçmişi"
      subtitle="Diğer bankalardan aldığınız ekstreleri yükleyin. Analizler bu geçmişten hesaplanır."
    />

    <app-statement-upload
      class="mb-8"
      [busy]="busy()"
      [result]="result()"
      [error]="error()"
      [details]="lineErrors()"
      (file)="store(api.importCsv($event))"
      (demo)="store(api.generateDemo(12, 42))"
      (rejected)="fail($event)"
    />

    <app-transaction-list
      [items]="items()"
      [totalCount]="totalCount()"
      [page]="page()"
      [pageSize]="pageSize"
      (filter)="applyFilter($event)"
      (pageChange)="goTo($event)"
    />
  `,
})
/** Container: uploads statements, generates demo data and pages through the imported lines. */
export class TransactionsPageComponent extends BasePage {
  protected readonly api = inject(TransactionsApi);

  protected readonly pageSize = PAGE_SIZE;
  protected readonly items = signal<HistoryTransaction[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);
  private range: DateRange = {};

  protected readonly result = signal<ImportResult | null>(null);
  protected readonly lineErrors = signal<string[]>([]);

  constructor() {
    super();
    this.fetchPage();
  }

  protected store(call: Observable<ImportResult>): void {
    this.result.set(null);
    this.fail(null);

    this.act(call, {
      fallback: 'Dosya yüklenemedi.',
      onDone: (result) => {
        this.result.set(result);
        this.applyFilter(this.range);
      },
      onError: (err) => this.lineErrors.set(problemErrors(err)),
    });
  }

  protected applyFilter(range: DateRange): void {
    this.range = range;
    this.page.set(1);
    this.fetchPage();
  }

  protected goTo(page: number): void {
    this.page.set(page);
    this.fetchPage();
  }

  protected fail(message: string | null, lines: string[] = []): void {
    this.error.set(message);
    this.lineErrors.set(lines);
  }

  private fetchPage(): void {
    this.load(
      this.api.list(this.page(), PAGE_SIZE, this.range.from, this.range.to),
      (page) => {
        this.items.set(page.items);
        this.totalCount.set(page.totalCount);
      },
      'İşlemler yüklenemedi.',
    );
  }
}

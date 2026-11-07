import { Component, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';

import { problemErrors, problemMessage } from '../../core/http/problem-details';
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
export class TransactionsPageComponent {
  protected readonly api = inject(TransactionsApi);

  protected readonly pageSize = PAGE_SIZE;
  protected readonly items = signal<HistoryTransaction[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);
  private range: DateRange = {};

  protected readonly busy = signal(false);
  protected readonly result = signal<ImportResult | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly lineErrors = signal<string[]>([]);

  constructor() {
    this.load();
  }

  protected store(call: Observable<ImportResult>): void {
    this.busy.set(true);
    this.result.set(null);
    this.fail(null);

    call.subscribe({
      next: (result) => {
        this.busy.set(false);
        this.result.set(result);
        this.applyFilter(this.range);
      },
      error: (err) => {
        this.busy.set(false);
        this.fail(problemMessage(err, 'Dosya yüklenemedi.'), problemErrors(err));
      },
    });
  }

  protected applyFilter(range: DateRange): void {
    this.range = range;
    this.page.set(1);
    this.load();
  }

  protected goTo(page: number): void {
    this.page.set(page);
    this.load();
  }

  protected fail(message: string | null, lines: string[] = []): void {
    this.error.set(message);
    this.lineErrors.set(lines);
  }

  private load(): void {
    this.api.list(this.page(), PAGE_SIZE, this.range.from, this.range.to).subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.totalCount.set(page.totalCount);
      },
      error: (err) => this.fail(problemMessage(err, 'İşlemler yüklenemedi.')),
    });
  }
}

import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';

import { problemErrors, problemMessage } from '../../core/http/problem-details';
import { TransactionsApi } from '../../data-access/api/finance.api';
import { HistoryTransaction, ImportResult } from '../../data-access/models/finance.models';

const PAGE_SIZE = 50;
const MAX_FILE_BYTES = 2 * 1024 * 1024;

@Component({
  selector: 'app-transactions-page',
  imports: [FormsModule, CurrencyPipe, DatePipe, NgClass],
  templateUrl: './transactions-page.component.html',
})
export class TransactionsPageComponent {
  private readonly api = inject(TransactionsApi);

  protected readonly items = signal<HistoryTransaction[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);
  protected readonly from = signal('');
  protected readonly to = signal('');
  protected readonly loading = signal(true);

  protected readonly busy = signal(false);
  protected readonly result = signal<ImportResult | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly lineErrors = signal<string[]>([]);

  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)));

  constructor() {
    this.load();
  }

  protected onFileSelected(input: HTMLInputElement): void {
    const file = input.files?.[0];
    input.value = ''; // allows choosing the same file again after fixing it
    if (!file) return;

    // Checked here too so the user is not made to wait for an upload the API would refuse.
    if (file.size > MAX_FILE_BYTES) {
      this.showFailure('Dosya en fazla 2 MB olabilir.');
      return;
    }

    this.store(this.api.importCsv(file));
  }

  protected generateDemo(): void {
    this.store(this.api.generateDemo(12, 42));
  }

  protected applyFilter(): void {
    this.page.set(1);
    this.load();
  }

  protected goTo(page: number): void {
    this.page.set(page);
    this.load();
  }

  private store(call: Observable<ImportResult>): void {
    this.busy.set(true);
    this.result.set(null);
    this.error.set(null);
    this.lineErrors.set([]);

    call.subscribe({
      next: (result) => {
        this.busy.set(false);
        this.result.set(result);
        this.applyFilter();
      },
      error: (err) => {
        this.busy.set(false);
        this.showFailure(problemMessage(err, 'Dosya yüklenemedi.'), problemErrors(err));
      },
    });
  }

  private showFailure(message: string, lines: string[] = []): void {
    this.error.set(message);
    this.lineErrors.set(lines);
  }

  private load(): void {
    this.loading.set(true);
    this.api.list(this.page(), PAGE_SIZE, this.from() || undefined, this.to() || undefined).subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.showFailure(problemMessage(err, 'İşlemler yüklenemedi.'));
        this.loading.set(false);
      },
    });
  }
}

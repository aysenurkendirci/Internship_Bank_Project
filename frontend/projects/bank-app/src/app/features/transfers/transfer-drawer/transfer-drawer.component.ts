import { Component, EventEmitter, Input, Output, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { DashboardApi } from '../../../data-access/api/dashboard.api';
import { TransfersApi } from '../../../data-access/api/transfers.api';

type Mode = 'between-accounts' | 'account-to-card' | 'external-iban';

@Component({
  selector: 'app-transfer-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './transfer-drawer.component.html',
})
export class TransferDrawerComponent implements OnChanges {
  private dashboardApi = inject(DashboardApi);
  private transfersApi = inject(TransfersApi);

  @Input() open = false;
  @Input() mode: Mode = 'between-accounts';

  @Input() defaultFromAccountId: number | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() success = new EventEmitter<void>();

  isLoading = false;

  accounts: Array<any> = [];
  cards: Array<any> = [];

  fromAccountId: number | null = null;

  toAccountId: number | null = null;
  toCardId: number | null = null;

  amount: number | null = null;
  note: string | null = null;

  receiverFullName: string | null = null;
  receiverIban: string | null = null;

  status: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']?.currentValue === true) {
      this.bootstrap();
    }
    if (changes['defaultFromAccountId']?.currentValue) {
      this.fromAccountId = this.defaultFromAccountId;
    }
  }

  bootstrap() {
    this.status = null;
    this.amount = null;
    this.note = null;

    this.toAccountId = null;
    this.toCardId = null;

    this.receiverFullName = null;
    this.receiverIban = null;

    this.fromAccountId = this.defaultFromAccountId;

    this.loadLists();
  }

  loadLists() {
    this.isLoading = true;
    this.dashboardApi.getDashboard()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (res: any) => {
          this.accounts = res?.accounts ?? [];
          this.cards = res?.cards ?? [];
        },
        error: (err) => {
          console.error('Dashboard list load error:', err);
          this.status = 'Liste yüklenemedi.';
        },
      });
  }

  close() {
    this.closed.emit();
  }

  submit() {
    this.status = null;

    if (!this.fromAccountId || this.fromAccountId <= 0) {
      this.status = 'Gönderen hesabı seçmelisin.';
      return;
    }
    if (!this.amount || this.amount <= 0) {
      this.status = 'Geçerli bir tutar gir.';
      return;
    }
    if (this.mode === 'external-iban') {
      this.status = 'Dış transfer için backend endpoint gerekli (IBAN ile).';
      return;
    }
    if (this.mode === 'between-accounts') {
      if (!this.toAccountId || this.toAccountId <= 0) {
        this.status = 'Hedef hesabı seçmelisin.';
        return;
      }
      if (this.toAccountId === this.fromAccountId) {
        this.status = 'Gönderen ve hedef hesap aynı olamaz.';
        return;
      }

      this.isLoading = true;
      this.transfersApi.create({
        fromAccountId: this.fromAccountId,
        toAccountId: this.toAccountId,
        toCardId: null,
        amount: this.amount,
        note: this.note,
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.status = 'Başarılı!';
          setTimeout(() => {
            this.success.emit();
            this.close();
          }, 600);
        },
        error: (err) => {
          console.error(err);
          this.status = 'İşlem başarısız.';
        },
      });

      return;
    }
    if (this.mode === 'account-to-card') {
      if (!this.toCardId || this.toCardId <= 0) {
        this.status = 'Hedef kartı seçmelisin.';
        return;
      }

      this.isLoading = true;
      this.transfersApi.create({
        fromAccountId: this.fromAccountId,
        toAccountId: null,
        toCardId: this.toCardId,
        amount: this.amount,
        note: this.note,
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.status = 'Başarılı!';
          setTimeout(() => {
            this.success.emit();
            this.close();
          }, 600);
        },
        error: (err) => {
          console.error(err);
          this.status = 'İşlem başarısız.';
        },
      });
    }
  }
}

import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { AccountsApi, AccountDetailResponse } from '../../../data-access/api/accounts.api';
import { TransactionsApi, TransactionItem } from '../../../data-access/api/transactions.api';
import { TransferDrawerComponent } from '../../transfers/transfer-drawer/transfer-drawer.component';

type TransferMode = 'between-accounts' | 'account-to-card' | 'external-iban';

type WeeklyBar = {
  label: string;
  amount: number;
  height: number; // 0-100
};

@Component({
  selector: 'app-accounts-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, TransferDrawerComponent],
  templateUrl: './accounts-detail.component.html',
})
export class AccountsDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private accountsApi = inject(AccountsApi);
  private txApi = inject(TransactionsApi);
  private location = inject(Location);
  private cdr = inject(ChangeDetectorRef);

  id = 0;

  account?: AccountDetailResponse;
  txs: TransactionItem[] = [];

  drawerOpen = false;
  drawerMode: TransferMode = 'between-accounts';

  // Son işlemler görünümü
  previewLimit = 8;
  showAllTx = false;

  // Mini chart
  weeklySpendBars: WeeklyBar[] = [];
  last7DaysTotalOut = 0;

  ngOnInit() {
    // Route param değişince hesap + işlemler yeniden çekilir
    this.route.paramMap.subscribe((params) => {
      this.id = Number(params.get('id') ?? 0);
      if (this.id > 0) this.fetchAccountDetails(this.id);
    });
  }

  private fetchAccountDetails(id: number) {
    this.accountsApi.getById(id)
      .pipe(finalize(() => this.cdr.detectChanges()))
      .subscribe({
        next: (res) => {
          this.account = res;

          // Hesap detayı geldikten sonra işlemleri çek
          this.fetchRecentTxByAccount(id);
        },
        error: (err) => console.error('Hesap detay hatası:', err),
      });
  }

  private fetchRecentTxByAccount(accountId: number) {
    this.txApi.recentByAccount(accountId, 20).subscribe({
      next: (txs) => {
        this.txs = txs ?? [];

        // Yeni işlemler gelince chart datasını yeniden oluştur
        this.buildWeeklySpendChart(this.txs);

        this.cdr.detectChanges();
      },
      error: (err) => console.error('İşlem geçmişi çekme hatası:', err),
    });
  }

  onTransferSuccess() {
    // Transfer sonrası bakiye + işlemler güncellensin
    this.fetchAccountDetails(this.id);
  }

  openTransfer(mode: TransferMode) {
    this.drawerMode = mode;
    this.drawerOpen = true;
  }

  closeDrawer() {
    this.drawerOpen = false;
  }

  goBack() {
    this.location.back();
  }

  // -----------------------
  // UI getters
  // -----------------------
  get accountNoSafe(): string {
    return this.account?.iban ?? '—';
  }

  get accountStatusSafe(): string {
    return this.account?.status ?? '—';
  }

  get accountBalanceSafe(): number {
    // balance string gelirse de güvenli parse et
    const raw: any = this.account?.balance;
    const n =
      typeof raw === 'string'
        ? Number(raw.replace(/[^\d.-]/g, ''))
        : Number(raw ?? 0);

    return Number.isFinite(n) ? n : 0;
  }

  get accountTypeLabel(): string {
    const raw = (this.account?.type ?? '').toUpperCase();
    if (!raw) return '—';

    // basit mapping (gerekirse genişletirsin)
    if (raw.includes('VADELI')) return 'VADELİ_TL';
    if (raw.includes('VADESIZ')) return 'VADESİZ_TL';

    return raw;
  }

  // -----------------------
  // Son işlemler helpers
  // -----------------------
  get visibleTxs(): TransactionItem[] {
    return this.showAllTx ? this.txs : this.txs.slice(0, this.previewLimit);
  }

  get totalTxCount(): number {
    return this.txs.length;
  }

  // -----------------------
  // UX helper: IBAN copy
  // -----------------------
  copyIban() {
    const iban = this.account?.iban;
    if (!iban) return;

    try {
      navigator.clipboard.writeText(iban);
    } catch (e) {
      console.warn('Clipboard write failed:', e);
    }
  }

  // -----------------------
  // Mini chart: Son 7 gün OUT harcama
  // -----------------------
  private buildWeeklySpendChart(txs: TransactionItem[]) {
    const now = new Date();
    const dayMS = 24 * 60 * 60 * 1000;

    const buckets: { dateKey: string; label: string; amount: number }[] = [];

    // Son 7 gün (bugün dahil)
    for (let i = 6; i >= 0; i--) {
      const d = new Date(now.getTime() - i * dayMS);
      buckets.push({
        dateKey: this.toDateKey(d),
        label: this.dayLabelTR(d),
        amount: 0,
      });
    }

    // OUT işlemleri günü gününe topla
    for (const t of txs) {
      if (t.direction !== 'OUT') continue;
      if (!t.createdAt) continue;

      const key = this.toDateKey(new Date(t.createdAt));
      const bucket = buckets.find(b => b.dateKey === key);
      if (bucket) bucket.amount += Number(t.amount ?? 0);
    }

    const max = Math.max(...buckets.map(b => b.amount), 0);
    this.last7DaysTotalOut = buckets.reduce((sum, b) => sum + b.amount, 0);

    this.weeklySpendBars = buckets.map(b => {
      // hiç harcama yoksa bile bar görünsün
      if (max === 0) return { label: b.label, amount: b.amount, height: 8 };

      const pct = (b.amount / max) * 100;
      const height = b.amount > 0 ? Math.max(12, Math.round(pct)) : 8;

      return { label: b.label, amount: b.amount, height };
    });
  }

  private toDateKey(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private dayLabelTR(d: Date): string {
    const days = ['Paz', 'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cts'];
    return days[d.getDay()] ?? '';
  }
}

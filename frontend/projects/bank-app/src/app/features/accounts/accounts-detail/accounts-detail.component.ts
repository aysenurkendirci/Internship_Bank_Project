import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

// Servisler ve Tipler
import { AccountsApi } from '../../../data-access/api/accounts.api';
import { TransactionsApi, TransactionItem } from '../../../data-access/api/transactions.api'; 

// Bileşenler
import { TransferDrawerComponent } from '../../transfers/transfer-drawer/transfer-drawer.component';

@Component({
  selector: 'app-accounts-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, TransferDrawerComponent],
  templateUrl: './accounts-detail.component.html',
})
export class AccountsDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private accountsApi = inject(AccountsApi);
  private txApi = inject(TransactionsApi); // ✅ Yeni servis inject edildi
  private location = inject(Location);
  private cdr = inject(ChangeDetectorRef);

  id = 0;
  account?: any;
  txs: TransactionItem[] = []; // ✅ Tip güvenliği eklendi

  drawerOpen = false;
  drawerMode: 'between-accounts' | 'account-to-card' | 'external-iban' = 'between-accounts';

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      this.id = Number(params.get('id') ?? 0);
      if (this.id > 0) this.fetchAccountDetails(this.id);
    });
  }

  fetchAccountDetails(id: number) {
    this.accountsApi.getById(id)
      .pipe(finalize(() => this.cdr.detectChanges()))
      .subscribe({
        next: (res: any) => {
          this.account = res;
          
          // ✅ 1) Account detail geldikten sonra işlemleri ayrı çekiyoruz
          this.fetchRecentTxByAccount(id);
        },
        error: (err) => console.error('Hesap detay hatası:', err)
      });
  }

  // ✅ 2) İşlemleri (Transactions) çeken yeni metod
  private fetchRecentTxByAccount(accountId: number) {
    this.txApi.recentByAccount(accountId, 20).subscribe({
      next: (txs) => {
        this.txs = txs ?? [];
        this.cdr.detectChanges(); // Veri geldiğinde UI'ı tetikle
      },
      error: (err) => console.error('İşlem geçmişi çekme hatası:', err)
    });
  }

  onTransferSuccess() {
    // Hem hesap bakiyesi hem de yeni işlem listesi güncellenir
    this.fetchAccountDetails(this.id);
  }

  // --- Helper Getters ---
  openTransfer(mode: 'between-accounts' | 'account-to-card' | 'external-iban') {
    this.drawerMode = mode;
    this.drawerOpen = true;
  }

  closeDrawer() {
    this.drawerOpen = false;
  }

  get accountNameSafe(): string { return this.account?.accountName ?? this.account?.type ?? '—'; }
  get accountNoSafe(): string { return this.account?.accountNo ?? this.account?.iban ?? '—'; }
  get accountStatusSafe(): string { return this.account?.status ?? '—'; }
  get accountBalanceSafe(): number { return Number(this.account?.balance ?? 0); }

  goBack() { this.location.back(); }
}
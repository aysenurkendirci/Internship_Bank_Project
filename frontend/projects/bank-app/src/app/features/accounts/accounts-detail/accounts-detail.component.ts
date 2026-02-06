import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AccountsApi } from '../../../data-access/api/accounts.api';
import { finalize } from 'rxjs';
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
  private location = inject(Location);
  private cdr = inject(ChangeDetectorRef);

  id = 0;
  account?: any;
  txs: any[] = [];

  // Drawer State
  drawerOpen = false;
  drawerMode: 'between-accounts' | 'account-to-card' | 'external-iban' = 'between-accounts';

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      this.id = Number(params.get('id') ?? 0);
      if (this.id > 0) {
        this.fetchAccountDetails(this.id);
      }
    });
  }

  fetchAccountDetails(id: number) {
    this.accountsApi.getById(id)
      .pipe(finalize(() => this.cdr.detectChanges()))
      .subscribe({
        next: (res: any) => {
          this.account = res;
          this.txs = res.transactions || [];
        },
        error: (err) => console.error('Veri çekme hatası:', err)
      });
  }

  openTransfer(mode: 'between-accounts' | 'account-to-card' | 'external-iban') {
    this.drawerMode = mode;
    this.drawerOpen = true;
  }

  closeDrawer() {
    this.drawerOpen = false;
  }

  onTransferSuccess() {
    this.fetchAccountDetails(this.id);
  }

  get accountNameSafe(): string { return this.account?.type ?? '—'; }
  get accountNoSafe(): string { return this.account?.iban ?? '—'; }
  get accountStatusSafe(): string { return this.account?.status ?? '—'; }
  get accountBalanceSafe(): number { return Number(this.account?.balance ?? 0); }

  goBack() { this.location.back(); }
}
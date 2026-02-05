import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AccountsApi } from '../../../data-access/api/accounts.api';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-accounts-detail',
  standalone: true,
  imports: [CommonModule, RouterLink], // DatePipe uyarısı için buradan çıkarıldı
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

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      this.id = Number(params.get('id') ?? 0);
      if (this.id > 0) {
        this.fetchAccountDetails(this.id);
      }
    });
  }

  private fetchAccountDetails(id: number) {
    this.accountsApi.getById(id)
      .pipe(finalize(() => {
        this.cdr.detectChanges(); // Veri geldiğinde ekranı güncelle
      }))
      .subscribe({
        next: (res) => {
          this.account = res;
          this.txs = (res as any).transactions || [];
          console.log('Hesap Detay:', res);
        },
        error: (err) => console.error('Hesap hatası:', err)
      });
  }

  get accountNameSafe(): string { return this.account?.type ?? '—'; }
  get accountNoSafe(): string { return this.account?.iban ?? '—'; }
  get accountStatusSafe(): string { return this.account?.status ?? '—'; }
  get accountBalanceSafe(): number { return Number(this.account?.balance ?? 0); }

  goBack() { this.location.back(); }
}
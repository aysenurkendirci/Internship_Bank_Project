import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AccountsApi, AccountDetailResponse } from '../../../data-access/api/accounts.api'; // path'i senin projene göre gerekirse düzelt
import { take } from 'rxjs/operators';

type TxItem = {
  category: string;
  createdAt: string | Date;
  direction: 'IN' | 'OUT';
  amount: number;
  description?: string | null;
};

@Component({
  selector: 'app-accounts-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './accounts-detail.component.html',
})
export class AccountsDetailComponent {
  private route = inject(ActivatedRoute);
  private accountsApi = inject(AccountsApi);

  id = Number(this.route.snapshot.paramMap.get('id') ?? 0);

  // ✅ HTML’de bind edeceğimiz data
  account?: AccountDetailResponse;

  txs: TxItem[] = [];

  ngOnInit() {
    if (!this.id) return;

    this.accountsApi.getById(this.id).pipe(take(1)).subscribe({
      next: (res) => {
        console.log('Account detail response:', res);
        this.account = res;
      },
      error: (err) => {
        console.error('Account detail error:', err);
      },
    });
  }
}

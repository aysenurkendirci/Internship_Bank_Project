import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardApi } from '../../data-access/api/dashboard.api';

type DashboardVm = {
  user: { userId: number; firstName: string; membership: string };
  totalWealth: number;
  wealthChangeRate: number;
  cards: Array<{
    cardId: number;
    cardNoMasked: string;
    cardType: string;
    isVirtual: boolean;
    status: string;
    balance: number;
  }>;
  recentTransactions: Array<{
    txId: number;
    title: string;
    category: string;
    amount: number;
    direction: 'IN' | 'OUT' | string;
    createdAt: string; // backend DateTime -> ISO
  }>;
  accounts: Array<{
    title: string;
    subtitle: string;
    balance: number;
    statusText: string;
  }>;
};

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private api = inject(DashboardApi);

  vm: DashboardVm = {
    user: { userId: 0, firstName: '', membership: '' },
    totalWealth: 0,
    wealthChangeRate: 0,
    cards: [],
    recentTransactions: [],
    accounts: [],
  };

  ngOnInit(): void {
    this.api.getDashboard().subscribe({
      next: (res: any) => {
        // backend artık camelCase dönecek (Program.cs fix’i ile)
        const data = res as Omit<DashboardVm, 'accounts'>;

        const accounts = (data.cards ?? []).slice(0, 3).map((c, i) => ({
          title: c.cardType || (c.isVirtual ? 'Sanal Kart' : 'Kart'),
          subtitle: `**** ${c.cardNoMasked?.slice(-4) ?? ''}`,
          balance: c.balance ?? 0,
          statusText: (c.status || 'Aktif'),
        }));

        this.vm = { ...data, accounts };
      },
      error: (err) => {
        console.error('Dashboard API hatası:', err);
      },
    });
  }

  cardGradient(i: number): string {
    const gradients = ['elegant-gradient-1', 'elegant-gradient-2', 'elegant-gradient-3'];
    return gradients[i % gradients.length];
  }

  txAmountClass(tx: any) {
    return tx.direction === 'IN' ? 'text-emerald-600' : 'text-slate-900';
  }

  txSign(tx: any) {
    return tx.direction === 'IN' ? '+' : '-';
  }
}

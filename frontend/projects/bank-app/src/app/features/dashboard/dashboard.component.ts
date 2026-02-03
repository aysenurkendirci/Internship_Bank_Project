import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DashboardApi } from '../../data-access/api/dashboard.api';

type DashboardResponse = {
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
    direction: string;
    createdAt: string;
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
  private router = inject(Router);

  loading = false;
  error?: string;

  // Mentör stili: Başlangıçta değer atamıyoruz (undefined)
  vm?: DashboardResponse; 

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.loading = true;
    this.error = undefined;

    this.api.getDashboard().subscribe({
      next: (res: DashboardResponse) => {
        // Veri geldiği an vm dolar ve HTML tetiklenir
        this.vm = res;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        if ((err as any)?.status === 401) {
          localStorage.removeItem('token');
          this.router.navigateByUrl('/auth/login');
          return;
        }
        this.error = 'Dashboard verileri alınamadı.';
      },
    });
  }

  // [ngClass] için köşeli parantez bağlaması
  cardGradient(i: number): string {
    const gradients = ['bg-slate-900', 'bg-blue-600', 'bg-indigo-500'];
    return gradients[i % gradients.length];
  }
}
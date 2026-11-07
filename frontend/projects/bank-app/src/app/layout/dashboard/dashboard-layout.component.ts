import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { SessionService } from '../../core/auth/session.service';
import { AccountApi } from '../../data-access/api/account.api';

interface NavItem {
  readonly path: string;
  readonly label: string;
  readonly icon: string;
}

@Component({
  selector: 'app-dashboard-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './dashboard-layout.component.html',
})
export class DashboardLayoutComponent {
  private readonly session = inject(SessionService);
  private readonly router = inject(Router);
  private readonly account = inject(AccountApi);

  protected readonly exporting = signal(false);

  protected readonly fullName = this.session.fullName;

  protected readonly navItems: readonly NavItem[] = [
    { path: '/dashboard', label: 'Ana Sayfa', icon: 'grid_view' },
    { path: '/dashboard/cash-flow', label: 'Nakit akışı', icon: 'timeline' },
    { path: '/dashboard/scenarios', label: 'Taksit senaryosu', icon: 'compare_arrows' },
    { path: '/dashboard/recurring', label: 'Gelir ve giderler', icon: 'event_repeat' },
    { path: '/dashboard/cards', label: 'Kartlar ve taksitler', icon: 'credit_card' },
    { path: '/dashboard/transactions', label: 'İşlem geçmişi', icon: 'receipt_long' },
    { path: '/dashboard/analytics', label: 'Analiz', icon: 'insights' },
  ];

  /** Saves the personal data export as a file; the browser's download handles the rest. */
  exportMyData(): void {
    this.exporting.set(true);
    this.account.exportPersonalData().subscribe({
      next: (file) => {
        const url = URL.createObjectURL(file);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'kisisel-verilerim.json';
        link.click();
        URL.revokeObjectURL(url);
        this.exporting.set(false);
      },
      error: () => this.exporting.set(false),
    });
  }

  logout(): void {
    this.session.end();
    this.router.navigateByUrl('/auth/login');
  }
}

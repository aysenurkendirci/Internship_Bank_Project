import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { SessionService } from '../../core/auth/session.service';
import { ConfirmDialogComponent } from '../../shared/ui/confirm-dialog.component';
import { ToastHostComponent } from '../../shared/ui/toast-host.component';

interface NavItem {
  readonly path: string;
  readonly label: string;
  readonly icon: string;
}

interface NavGroup {
  readonly title: string;
  readonly items: readonly NavItem[];
}

/** The frame of the signed-in app: the menu, the user's corner, and the dialog and toast outlets. */
@Component({
  selector: 'app-dashboard-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ConfirmDialogComponent, ToastHostComponent],
  templateUrl: './dashboard-layout.component.html',
})
export class DashboardLayoutComponent {
  private readonly session = inject(SessionService);
  private readonly router = inject(Router);

  protected readonly fullName = this.session.fullName;

  protected readonly navGroups: readonly NavGroup[] = [
    {
      title: 'Bankacılık',
      items: [
        { path: '/dashboard', label: 'Ana Sayfa', icon: 'home' },
        { path: '/dashboard/accounts', label: 'Hesaplarım', icon: 'account_balance' },
        { path: '/dashboard/transfer', label: 'Para transferi', icon: 'send_money' },
        { path: '/dashboard/bank-cards', label: 'Banka kartlarım', icon: 'payments' },
      ],
    },
    {
      title: 'Planlama',
      items: [
        { path: '/dashboard/cash-flow', label: 'Nakit akışı', icon: 'timeline' },
        { path: '/dashboard/scenarios', label: 'Taksit senaryosu', icon: 'compare_arrows' },
        { path: '/dashboard/recurring', label: 'Gelir ve giderler', icon: 'event_repeat' },
        { path: '/dashboard/cards', label: 'Kartlar ve taksitler', icon: 'credit_card' },
        { path: '/dashboard/transactions', label: 'İşlem geçmişi', icon: 'receipt_long' },
        { path: '/dashboard/analytics', label: 'Analiz', icon: 'insights' },
      ],
    },
  ];

  protected logout(): void {
    this.session.signOut().subscribe(() => this.router.navigateByUrl('/auth/login'));
  }
}

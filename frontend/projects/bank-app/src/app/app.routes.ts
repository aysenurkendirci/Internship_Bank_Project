import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';

// Every page is loaded on demand, so the first download stays small.
export const routes: Routes = [
  {
    path: 'auth/login',
    title: 'Giriş yap',
    loadComponent: () =>
      import('./features/auth/pages/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'auth/register',
    title: 'Kayıt ol',
    loadComponent: () =>
      import('./features/auth/pages/register/register.component').then(m => m.RegisterComponent),
  },

  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/dashboard/dashboard-layout.component').then(m => m.DashboardLayoutComponent),
    children: [
      {
        path: '',
        title: 'Ana Sayfa',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
      {
        path: 'accounts',
        title: 'Hesaplarım',
        loadComponent: () => import('./features/accounts/accounts-page.component').then((m) => m.AccountsPageComponent),
      },
      {
        path: 'accounts/:id',
        title: 'Hesap hareketleri',
        loadComponent: () =>
          import('./features/accounts/account-statement-page.component').then((m) => m.AccountStatementPageComponent),
      },
      {
        path: 'transfer',
        title: 'Para transferi',
        loadComponent: () => import('./features/transfer/transfer-page.component').then((m) => m.TransferPageComponent),
      },
      {
        path: 'bank-cards',
        title: 'Banka kartlarım',
        loadComponent: () => import('./features/bank-cards/bank-cards-page.component').then((m) => m.BankCardsPageComponent),
      },
      {
        path: 'profile',
        title: 'Hesap ayarları',
        loadComponent: () => import('./features/profile/profile-page.component').then((m) => m.ProfilePageComponent),
      },
      {
        path: 'cash-flow',
        title: 'Nakit akışı',
        loadComponent: () => import('./features/cash-flow/cash-flow-page.component').then((m) => m.CashFlowPageComponent),
      },
      {
        path: 'scenarios',
        title: 'Taksit senaryosu',
        loadComponent: () => import('./features/scenarios/scenarios-page.component').then((m) => m.ScenariosPageComponent),
      },
      {
        path: 'cards',
        title: 'Kartlar ve taksitler',
        loadComponent: () => import('./features/cards/cards-page.component').then((m) => m.CardsPageComponent),
      },
      {
        path: 'analytics',
        title: 'Analiz',
        loadComponent: () => import('./features/analytics/analytics-page.component').then((m) => m.AnalyticsPageComponent),
      },
      {
        path: 'transactions',
        title: 'İşlem geçmişi',
        loadComponent: () =>
          import('./features/transactions/transactions-page.component').then((m) => m.TransactionsPageComponent),
      },
      {
        path: 'recurring',
        title: 'Düzenli gelir ve giderler',
        loadComponent: () =>
          import('./features/recurring/recurring-page.component').then((m) => m.RecurringPageComponent),
      },
    ],
  },

  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: '**',
    title: 'Sayfa bulunamadı',
    loadComponent: () => import('./features/not-found/not-found.component').then((m) => m.NotFoundComponent),
  },
];

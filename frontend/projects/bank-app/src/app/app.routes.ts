import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';


// Every page is loaded on demand, so the first download stays small.
export const routes: Routes = [
  {
    path: 'auth/login',
    loadComponent: () =>
      import('./features/auth/pages/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'auth/register',
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
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
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

  { path: '', redirectTo: 'auth/login', pathMatch: 'full' },
  { path: '**', redirectTo: 'auth/login' },
];

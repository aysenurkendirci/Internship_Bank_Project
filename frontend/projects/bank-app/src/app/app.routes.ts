import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'auth/login' },

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
        pathMatch: 'full',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
      {
        path: 'cards/:id',
        loadComponent: () =>
          import('./features/cards/card-detail/card-detail.component').then(m => m.CardDetailComponent),
      },
      {
        path: 'accounts/:id',
        loadComponent: () =>
          import('./features/accounts/accounts-detail/accounts-detail.component').then(m => m.AccountsDetailComponent),
      },
      // DOĞRU YER: Children dizisinin içinde olmalı
      {
        path: 'savings-goals',
        loadComponent: () =>
          import('./features/savings-goals/savings-goals.component').then(m => m.SavingsGoalsComponent),
      },
    ],
  },

  { path: '**', redirectTo: 'auth/login' },
];
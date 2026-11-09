import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** An address that does not exist in the app. */
@Component({
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="min-h-screen flex flex-col items-center justify-center px-4 text-center">
      <h1 class="page-title">Sayfa bulunamadı</h1>
      <p class="page-subtitle">Aradığınız adres değişmiş ya da hiç var olmamış olabilir.</p>
      <a routerLink="/dashboard" class="btn-primary mt-6">Ana sayfaya dön</a>
    </main>
  `,
})
export class NotFoundComponent {}

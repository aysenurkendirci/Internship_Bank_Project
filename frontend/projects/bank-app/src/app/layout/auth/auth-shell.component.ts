import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** The frame around the sign-in and sign-up forms: a brand panel on wide screens, the form beside it. */
@Component({
  selector: 'app-auth-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen lg:grid lg:grid-cols-2">
      <aside class="hidden lg:flex flex-col justify-between bg-brand text-white p-12">
        <span class="text-xl font-semibold tracking-tight">NeoBank</span>
        <div class="max-w-md">
          <p class="text-3xl font-semibold tracking-tight">Hesaplarınız ve önümüzdeki aylarınız tek yerde.</p>
          <p class="mt-4 text-white/70">Paranızı yönetin, kart ve taksitlerinizin bakiyenize etkisini önceden görün.</p>
        </div>
        <span class="text-sm text-white/50">Bu bir portföy projesidir, gerçek bir banka değildir.</span>
      </aside>

      <main class="flex items-center justify-center px-4 py-10">
        <div class="w-full max-w-md">
          <span class="lg:hidden block mb-8 text-xl font-semibold tracking-tight text-brand">NeoBank</span>
          <h1 class="page-title">{{ heading() }}</h1>
          <p class="page-subtitle mb-6">{{ subtitle() }}</p>
          <ng-content />
        </div>
      </main>
    </div>
  `,
})
export class AuthShellComponent {
  readonly heading = input.required<string>();
  readonly subtitle = input.required<string>();
}

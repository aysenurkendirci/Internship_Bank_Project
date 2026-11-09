import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import localeTr from '@angular/common/locales/tr';
import { ApplicationConfig, DEFAULT_CURRENCY_CODE, LOCALE_ID, inject, provideAppInitializer } from '@angular/core';
import { TitleStrategy, provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from '../app.routes';
import { authInterceptor } from '../core/auth/auth-interceptor';
import { SessionService } from '../core/auth/session.service';
import { PageTitleStrategy } from '../core/routing/page-title.strategy';

// Amounts and dates follow Turkish conventions everywhere: "₺1.234,56", "7 Kasım 2026".
registerLocaleData(localeTr);

// The app runs zoneless (Angular's default since v21): components keep state in signals so
// that changes made in HTTP callbacks are rendered without zone.js.
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    { provide: TitleStrategy, useClass: PageTitleStrategy },
    provideAppInitializer(() => inject(SessionService).restore()),
    { provide: LOCALE_ID, useValue: 'tr' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'TRY' },
  ],
};

import { ApplicationConfig } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from '../app.routes';
import { authInterceptor } from '../core/auth/auth-interceptor';

// The app runs zoneless (Angular's default since v21): components keep state in signals so
// that changes made in HTTP callbacks are rendered without zone.js.
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
  ],
};

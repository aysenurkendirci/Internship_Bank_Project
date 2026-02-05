import { bootstrapApplication } from '@angular/platform-browser';
import {
  provideRouter,
  withRouterConfig,
  withInMemoryScrolling,
} from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';

import { API_BASE_URL } from './app/core/api/api-base-url.token';
import { authInterceptor } from './app/core/auth/auth-interceptor';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(
      routes,
      withRouterConfig({ onSameUrlNavigation: 'reload' }),
      withInMemoryScrolling({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled',
      })
    ),
    provideHttpClient(withInterceptors([authInterceptor])),
    { provide: API_BASE_URL, useValue: 'http://localhost:5164' },
  ],
}).catch(console.error);

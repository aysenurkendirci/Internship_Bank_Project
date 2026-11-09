import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { SessionService } from './session.service';

/** Sends anyone without a session to the login page, remembering where they were going. */
export const authGuard: CanActivateFn = (_route, state) =>
  inject(SessionService).isAuthenticated()
    ? true
    : inject(Router).createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });

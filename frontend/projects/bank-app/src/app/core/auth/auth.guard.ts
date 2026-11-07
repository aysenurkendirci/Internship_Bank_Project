import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = () =>
  localStorage.getItem('token') ? true : inject(Router).parseUrl('/auth/login');

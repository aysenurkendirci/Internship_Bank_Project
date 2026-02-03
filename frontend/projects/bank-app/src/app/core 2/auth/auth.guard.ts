import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const token = localStorage.getItem('token');

  console.log('GUARD token=', token);

  if (!token) {
    router.navigateByUrl('/auth/login');
    return false;
  }

  return true;
};

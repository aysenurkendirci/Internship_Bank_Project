// src/app/core/auth/auth.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token'); // <-- KEY aynı olmalı

  // Auth endpointlerine token eklemek istemezsen:
  // if (req.url.includes('/api/auth/')) return next(req);

  if (!token) {
    return next(req);
  }

  const authReq = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });

  return next(authReq);
};

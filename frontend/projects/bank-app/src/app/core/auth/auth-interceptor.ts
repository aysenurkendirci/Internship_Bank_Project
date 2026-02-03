import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');

  console.log('[INTERCEPTOR]', req.url, 'hasToken=', !!token);

  if (!token) return next(req);

  const authReq = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });

  console.log('[INTERCEPTOR] Authorization added');

  return next(authReq);
};

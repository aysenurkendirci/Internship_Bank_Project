import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { authInterceptor } from './auth-interceptor';
import { SessionService } from './session.service';

const tick = () => new Promise((resolve) => setTimeout(resolve));
const unauthorized = { status: 401, statusText: 'Unauthorized' };

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let session: SessionService;

  beforeEach(() => {
    const stored = new Map<string, string>();
    Object.defineProperty(globalThis, 'localStorage', {
      configurable: true,
      value: {
        getItem: (key: string) => stored.get(key) ?? null,
        setItem: (key: string, value: string) => stored.set(key, value),
        removeItem: (key: string) => stored.delete(key),
      },
    });

    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: 'auth/login', children: [] }]), provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    session = TestBed.inject(SessionService);
    session.start({ token: 'old', fullName: 'Ayşe' });
  });

  it('renews an expired token and repeats the request', async () => {
    let body: unknown;
    http.get('/api/accounts').subscribe((response) => (body = response));

    backend.expectOne('/api/accounts').flush(null, unauthorized);
    await tick();
    backend.expectOne('/api/auth/refresh').flush({ token: 'new', fullName: 'Ayşe' });
    await tick();

    const retry = backend.expectOne('/api/accounts');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new');
    retry.flush([]);
    expect(body).toEqual([]);
  });

  it('exchanges the single-use cookie once for requests that fail together', async () => {
    http.get('/api/accounts').subscribe();
    http.get('/api/cards').subscribe();

    backend.expectOne('/api/accounts').flush(null, unauthorized);
    backend.expectOne('/api/cards').flush(null, unauthorized);
    await tick();

    backend.expectOne('/api/auth/refresh').flush({ token: 'new', fullName: 'Ayşe' });
    await tick();

    expect(backend.expectOne('/api/accounts').request.headers.get('Authorization')).toBe('Bearer new');
    expect(backend.expectOne('/api/cards').request.headers.get('Authorization')).toBe('Bearer new');
  });

  it('ends the session when the token cannot be renewed', async () => {
    let failed = false;
    http.get('/api/accounts').subscribe({ error: () => (failed = true) });

    backend.expectOne('/api/accounts').flush(null, unauthorized);
    await tick();
    backend.expectOne('/api/auth/refresh').flush(null, unauthorized);
    await tick();

    expect(failed).toBe(true);
    expect(session.isAuthenticated()).toBe(false);
  });

  it('keeps the session when renewal fails for an unrelated reason', async () => {
    let status = 0;
    http.get('/api/accounts').subscribe({ error: (e) => (status = e.status) });

    backend.expectOne('/api/accounts').flush(null, unauthorized);
    await tick();
    backend.expectOne('/api/auth/refresh').flush(null, { status: 503, statusText: 'Unavailable' });
    await tick();

    expect(status).toBe(503);
    expect(session.isAuthenticated()).toBe(true);
  });

  it('leaves a rejected sign-in to the form', async () => {
    let status = 0;
    http.post('/api/auth/login', {}).subscribe({ error: (e) => (status = e.status) });

    backend.expectOne('/api/auth/login').flush(null, unauthorized);
    await tick();

    backend.expectNone('/api/auth/refresh');
    expect(status).toBe(401);
    expect(session.isAuthenticated()).toBe(true);
  });
});

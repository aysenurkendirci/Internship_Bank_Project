import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';

/**
 * Base of every API client: one place for the HTTP client and for building URLs.
 *
 * URLs are relative on purpose. The dev server proxies /api to the backend and nginx does the same in
 * containers, so no host name ends up in the bundle.
 */
export abstract class BaseApi {
  protected readonly http = inject(HttpClient);

  /** The resource this client talks to, e.g. "/api/accounts". */
  protected abstract readonly root: string;

  protected url(path = ''): string {
    return `${this.root}${path}`;
  }
}

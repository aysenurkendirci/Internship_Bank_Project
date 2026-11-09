import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { BaseApi } from './base-api';

/** The signed-in customer's own data. */
@Injectable({ providedIn: 'root' })
export class AccountApi extends BaseApi {
  protected readonly root = '/api/me';

  /** Everything stored about the signed-in user, as a file (KVKK right of access). */
  exportPersonalData(): Observable<Blob> {
    return this.http.get(this.url('/data-export'), { responseType: 'blob' });
  }
}

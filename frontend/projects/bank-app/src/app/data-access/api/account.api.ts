import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AccountApi {
  private readonly http = inject(HttpClient);

  /** Everything stored about the signed-in user, as a file (KVKK right of access). */
  exportPersonalData(): Observable<Blob> {
    return this.http.get('/api/me/data-export', { responseType: 'blob' });
  }
}

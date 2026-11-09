import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AuthResponse } from '../models/auth.models';
import { ContactDetails, PasswordChange, Profile } from '../models/banking.models';
import { BaseApi } from './base-api';

/** The signed-in customer's own data. */
@Injectable({ providedIn: 'root' })
export class ProfileApi extends BaseApi {
  protected readonly root = '/api/me';

  get(): Observable<Profile> {
    return this.http.get<Profile>(this.url());
  }

  updateContact(details: ContactDetails): Observable<Profile> {
    return this.http.put<Profile>(this.url(), details);
  }

  /** Ends every other session; the answer is the new token for this one. */
  changePassword(change: PasswordChange): Observable<AuthResponse> {
    return this.http.put<AuthResponse>(this.url('/password'), change);
  }

  /** Everything stored about the signed-in user, as a file (KVKK right of access). */
  exportPersonalData(): Observable<Blob> {
    return this.http.get(this.url('/data-export'), { responseType: 'blob' });
  }

  /** KVKK right to erasure. Final, and refused while the accounts still hold money. */
  erase(password: string): Observable<void> {
    return this.http.delete<void>(this.url(), { body: { password } });
  }
}

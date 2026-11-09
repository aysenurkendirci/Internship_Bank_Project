import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { DashboardResponse } from '../models/dashboard.models';
import { BaseApi } from './base-api';

/** The home screen's data. */
@Injectable({ providedIn: 'root' })
export class DashboardApi extends BaseApi {
  protected readonly root = '/api/dashboard';

  getDashboard(): Observable<DashboardResponse> {
    return this.http.get<DashboardResponse>(this.url());
  }
}

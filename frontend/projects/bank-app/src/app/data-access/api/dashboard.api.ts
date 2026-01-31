import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { ENDPOINTS } from './endpoints';
import { DashboardResponse } from '../models/dashboard.models';


@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private http = inject(HttpClient);

  getSummary() {
    return this.http.get<DashboardResponse>(ENDPOINTS.dashboard.summary);
  }
}

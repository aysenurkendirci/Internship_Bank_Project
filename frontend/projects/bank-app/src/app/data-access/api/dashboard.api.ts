import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENDPOINTS } from './endpoints';

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private http = inject(HttpClient);

  getDashboard() {
    return this.http.get(ENDPOINTS.dashboard.summary);
  }
}

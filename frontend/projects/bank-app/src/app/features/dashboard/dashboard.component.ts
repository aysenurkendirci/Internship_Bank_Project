import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

import { DashboardApi } from '../../data-access/api/dashboard.api';
import { DashboardResponse } from '../../data-access/models/dashboard.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private api = inject(DashboardApi);

  data?: DashboardResponse;
  loading = true;

  ngOnInit() {
    this.api.getSummary().subscribe({
      next: (res) => { this.data = res; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }
}

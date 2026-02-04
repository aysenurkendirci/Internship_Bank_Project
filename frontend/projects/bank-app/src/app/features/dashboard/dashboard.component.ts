import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { DashboardApi } from '../../data-access/api/dashboard.api';
import { DashboardResponse } from '../../data-access/models/dashboard.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  private api = inject(DashboardApi);
  private router = inject(Router);

  loading = false;
  error?: string;
  vm?: DashboardResponse; 

  ngOnInit(): void {
    this.loadDashboardData();
  }

private loadDashboardData(): void {
  this.loading = true;
  this.api.getDashboard().subscribe({
    next: (res) => {
      console.log('Gelen Dashboard Verisi:', res); 
      this.vm = res;
      this.loading = false;
    },
    error: (err) => {
      console.error('Hata oluştu:', err);
      this.loading = false;
    }
  });
}

  cardGradient(i: number): string {
    const gradients = ['bg-slate-900', 'bg-blue-600', 'bg-indigo-500'];
    return gradients[i % gradients.length];
  }
}

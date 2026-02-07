import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { filter, finalize } from 'rxjs'; 
import { DashboardApi } from '../../data-access/api/dashboard.api';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  private api = inject(DashboardApi);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  loading = true; 
  error: string | null = null;
  vm?: any;

  ngOnInit(): void {
    this.loadDashboardData();

    this.route.fragment
      .pipe(filter((f): f is string => !!f))
      .subscribe((frag) => {
        this.scrollToElement(frag);
      });
  }

  private loadDashboardData(): void {
    this.loading = true;
    this.error = null;

    this.api.getDashboard()
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.detectChanges();
        
        // Veri yüklendikten sonra URL'de fragment varsa kaydır
        const currentFrag = this.route.snapshot.fragment;
        if (currentFrag) {
          this.scrollToElement(currentFrag);
        }
      }))
      .subscribe({
        next: (res) => {
          this.vm = res;
          console.log('Dashboard Verisi:', res);
        },
        error: (err) => { 
          console.error('Dashboard hatası:', err);
          this.error = 'Veriler yüklenirken bir hata oluştu.';
          this.loading = false;
        }
      });
  }

  private scrollToElement(id: string): void {
    setTimeout(() => {
      const el = document.getElementById(id);
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }
    }, 200);
  }

  cardGradient(i: number): string {
    const gradients = ['bg-slate-900', 'bg-blue-600', 'bg-indigo-500'];
    return gradients[i % gradients.length];
  }
}
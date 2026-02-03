import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './dashboard-layout.component.html', // ✅ Düzeltildi: dashboard.component.html değil
  styleUrl: './dashboard-layout.component.scss',
})
export class DashboardLayoutComponent {}
import { Component, inject, signal, OnInit } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { DashboardService, AdminDashboardData } from './dashboard.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-admin-dashboard',
  imports: [Sidebar, Topbar],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css'
})
export class AdminDashboard implements OnInit {
  private dashboardService = inject(DashboardService);
  private authService = inject(AuthService);

  sidebarOpen = signal(false);
  userName = this.authService.user()?.fullName ?? 'مدير النظام';
  data = signal<AdminDashboardData | null>(null);
  barHeights = signal<number[]>([]);
  maxBarHeight = 90;

  ngOnInit() {
    this.dashboardService.get().subscribe({
      next: (res) => {
        this.data.set(res);
        this.calcBarHeights(res.weeklyActivity);
      },
      error: (err) => {
        console.error('AdminDashboard API error:', err);
      }
    });
  }

  private calcBarHeights(activity: { day: string; count: number }[]) {
    const max = Math.max(...activity.map(a => a.count), 1);
    this.barHeights.set(activity.map(a => Math.max((a.count / max) * this.maxBarHeight, 5)));
  }
}

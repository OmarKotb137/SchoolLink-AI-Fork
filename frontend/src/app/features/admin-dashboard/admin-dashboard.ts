import { Component, inject, signal, ViewChild, ElementRef, OnInit, AfterViewInit } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { DashboardService, AdminDashboardData } from './dashboard.service';
import { AuthService } from '../../core/services/auth.service';
import { Chart, BarController, BarElement, CategoryScale, LinearScale, Tooltip, Legend, ArcElement, PointElement, LineElement, LineController, DoughnutController } from 'chart.js';

Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip, Legend, ArcElement, PointElement, LineElement, LineController, DoughnutController);

@Component({
  selector: 'app-admin-dashboard',
  imports: [Sidebar, Topbar],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css'
})
export class AdminDashboard implements OnInit, AfterViewInit {
  private dashboardService = inject(DashboardService);
  private authService = inject(AuthService);

  @ViewChild('activityChart') activityChartCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('distributionChart') distributionChartCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('trendChart') trendChartCanvas!: ElementRef<HTMLCanvasElement>;

  sidebarOpen = signal(false);
  userName = this.authService.user()?.fullName ?? 'مدير النظام';
  data = signal<AdminDashboardData | null>(null);

  private activityChart: Chart | null = null;
  private distributionChart: Chart | null = null;
  private trendChart: Chart | null = null;

  ngOnInit() {
    this.dashboardService.get().subscribe({
      next: (res) => {
        this.data.set(res);
      },
      error: (err) => {
        console.error('AdminDashboard API error:', err);
      }
    });
  }

  ngAfterViewInit() {
    setTimeout(() => this.buildCharts(), 300);
  }

  private buildCharts() {
    const d = this.data();
    if (!d) return;
    this.buildActivityChart(d.weeklyActivity);
    this.buildDistributionChart(d.totalStudents, d.totalTeachers, d.totalClasses);
    this.buildTrendChart(d.weeklyActivity, d.successRate);
  }

  private buildActivityChart(activity: { day: string; count: number }[]) {
    if (!this.activityChartCanvas) return;
    const ctx = this.activityChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;
    if (this.activityChart) this.activityChart.destroy();

    const days = activity.map(a => a.day);
    const counts = activity.map(a => a.count);

    const gradient = ctx.createLinearGradient(0, 0, 0, 200);
    gradient.addColorStop(0, '#6366f1');
    gradient.addColorStop(1, '#a5b4fc');

    this.activityChart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: days,
        datasets: [{
          label: 'النشاط',
          data: counts,
          backgroundColor: gradient,
          borderRadius: 8,
          borderSkipped: false,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            rtl: true,
            backgroundColor: '#1e1b4b',
            titleFont: { size: 13 },
            bodyFont: { size: 12 },
            padding: 10,
            cornerRadius: 8,
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            grid: { color: 'rgba(0,0,0,0.05)' },
            ticks: { stepSize: 1 }
          },
          x: {
            grid: { display: false }
          }
        }
      }
    });
  }

  private buildDistributionChart(students: number, teachers: number, classes: number) {
    if (!this.distributionChartCanvas) return;
    const ctx = this.distributionChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;
    if (this.distributionChart) this.distributionChart.destroy();

    this.distributionChart = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: ['الطلاب', 'المعلمين', 'الفصول'],
        datasets: [{
          data: [students, teachers, classes],
          backgroundColor: ['#6366f1', '#10b981', '#f59e0b'],
          borderWidth: 0,
          hoverOffset: 8,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '70%',
        plugins: {
          legend: {
            position: 'bottom',
            rtl: true,
            labels: {
              usePointStyle: true,
              padding: 16,
              font: { size: 12 },
            }
          },
          tooltip: {
            rtl: true,
            backgroundColor: '#1e1b4b',
            padding: 10,
            cornerRadius: 8,
            callbacks: {
              label: (ctx) => `${ctx.label}: ${ctx.parsed}`
            }
          }
        }
      }
    });
  }

  private buildTrendChart(activity: { day: string; count: number }[], successRate: number) {
    if (!this.trendChartCanvas) return;
    const ctx = this.trendChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;
    if (this.trendChart) this.trendChart.destroy();

    const days = activity.map(a => a.day.slice(0, 3));
    const counts = activity.map(a => a.count);

    const gradient = ctx.createLinearGradient(0, 0, 0, 180);
    gradient.addColorStop(0, 'rgba(99,102,241,0.3)');
    gradient.addColorStop(1, 'rgba(99,102,241,0.01)');

    this.trendChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: days,
        datasets: [{
          label: 'النشاط',
          data: counts,
          borderColor: '#6366f1',
          backgroundColor: gradient,
          fill: true,
          tension: 0.4,
          pointBackgroundColor: '#6366f1',
          pointBorderColor: '#fff',
          pointBorderWidth: 2,
          pointRadius: 4,
          pointHoverRadius: 6,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            rtl: true,
            backgroundColor: '#1e1b4b',
            padding: 10,
            cornerRadius: 8,
          }
        },
        scales: {
          y: { beginAtZero: true, grid: { color: 'rgba(0,0,0,0.05)' }, ticks: { stepSize: 1 } },
          x: { grid: { display: false } }
        }
      }
    });
  }
}

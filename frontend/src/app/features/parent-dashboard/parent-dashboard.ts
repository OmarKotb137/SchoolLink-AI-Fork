import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ParentDashboardChild, ParentDashboardData, ParentDashboardService } from '../../core/services/parent-dashboard.service';
import { AuthService } from '../../core/services/auth.service';
import { Chart, BarController, BarElement, CategoryScale, LinearScale, Tooltip, Legend, ArcElement, DoughnutController } from 'chart.js';

Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip, Legend, ArcElement, DoughnutController);

@Component({
  selector: 'app-parent-dashboard',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './parent-dashboard.html',
  styleUrl: './parent-dashboard.css'
})
export class ParentDashboard implements OnInit, AfterViewInit {
  private parentDashboardService = inject(ParentDashboardService);
  private authService = inject(AuthService);

  @ViewChild('performanceChart') performanceChartCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('absencesChart') absencesChartCanvas!: ElementRef<HTMLCanvasElement>;

  sidebarOpen = signal(false);
  displayUserName = this.authService.user()?.fullName || 'ولي الأمر';

  children = signal<ParentDashboardChild[]>([]);
  dashboardData = signal<ParentDashboardData | null>(null);
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);

  totalChildren = signal(0);
  activeChildren = signal(0);
  inactiveChildren = signal(0);

  private performanceChart: Chart | null = null;
  private absencesChart: Chart | null = null;

  ngOnInit(): void {
    this.loadData();
  }

  ngAfterViewInit() {
    setTimeout(() => this.buildCharts(), 400);
  }

  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.parentDashboardService.getMyChildren().subscribe({
      next: (res) => {
        const data = res?.data ?? res ?? [];
        this.children.set(Array.isArray(data) ? data : []);
        this.totalChildren.set(this.children().length);
        this.activeChildren.set(this.children().filter(c => c.isActive).length);
        this.inactiveChildren.set(this.children().filter(c => !c.isActive).length);

        this.parentDashboardService.getDashboard().subscribe({
          next: (dashRes) => {
            this.dashboardData.set(dashRes?.data ?? null);
            this.isLoading.set(false);
            setTimeout(() => this.buildCharts(), 200);
          },
          error: () => {
            this.isLoading.set(false);
          }
        });
      },
      error: (err) => {
        this.children.set([]);
        this.errorMessage.set(err?.message || 'تعذر تحميل البيانات');
        this.isLoading.set(false);
      }
    });
  }

  private buildCharts() {
    const d = this.dashboardData();
    if (!d || !d.children.length) return;
    this.buildPerformanceChart(d.children);
    this.buildAbsencesChart(d.children);
  }

  private buildPerformanceChart(children: { name: string; performance: number }[]) {
    if (!this.performanceChartCanvas) return;
    const ctx = this.performanceChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;
    if (this.performanceChart) this.performanceChart.destroy();

    this.performanceChart = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: children.map(c => c.name),
        datasets: [{
          data: children.map(c => c.performance || 10),
          backgroundColor: ['#6366f1', '#10b981', '#f59e0b', '#ec4899', '#06b6d4'],
          borderWidth: 0,
          hoverOffset: 8,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '65%',
        plugins: {
          legend: {
            position: 'bottom',
            rtl: true,
            labels: { usePointStyle: true, padding: 14, font: { size: 12 } }
          },
          tooltip: {
            rtl: true,
            backgroundColor: '#1e1b4b',
            padding: 10,
            cornerRadius: 8,
            callbacks: {
              label: (ctx) => `${ctx.label}: ${ctx.parsed}%`
            }
          }
        }
      }
    });
  }

  private buildAbsencesChart(children: { name: string; absences: number }[]) {
    if (!this.absencesChartCanvas) return;
    const ctx = this.absencesChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;
    if (this.absencesChart) this.absencesChart.destroy();

    const gradient = ctx.createLinearGradient(0, 0, 0, 200);
    gradient.addColorStop(0, '#f59e0b');
    gradient.addColorStop(1, '#fbbf24');

    this.absencesChart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: children.map(c => c.name),
        datasets: [{
          label: 'الغيابات',
          data: children.map(c => c.absences),
          backgroundColor: gradient,
          borderRadius: 8,
          borderSkipped: false,
          barPercentage: 0.35,
          categoryPercentage: 0.6,
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

  childStats(name: string) {
    return this.dashboardData()?.children.find(c => c.name === name);
  }
}

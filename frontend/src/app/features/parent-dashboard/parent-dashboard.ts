import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, ViewChild, ElementRef, AfterViewInit, effect } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import {
  ParentDashboardChild, ParentDashboardData, ParentDashboardService,
  ParentChildStats, ChildSubject, UpcomingExam, WeeklyPerformance, RecSection, FinalExamResult
} from '../../core/services/parent-dashboard.service';
import { AuthService } from '../../core/services/auth.service';
import { Chart, BarController, BarElement, CategoryScale, LinearScale, Tooltip, Legend, ArcElement, DoughnutController, LineController, LineElement, PointElement, Filler } from 'chart.js';

Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip, Legend, ArcElement, DoughnutController, LineController, LineElement, PointElement, Filler);

@Component({
  selector: 'app-parent-dashboard',
  standalone: true,
  imports: [CommonModule, Sidebar],
  templateUrl: './parent-dashboard.html',
  styleUrl: './parent-dashboard.css'
})
export class ParentDashboard implements OnInit, AfterViewInit {
  private parentDashboardService = inject(ParentDashboardService);
  private authService = inject(AuthService);

  @ViewChild('performanceChart') performanceChartCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('absencesChart') absencesChartCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('weeklyChart') weeklyChartCanvas!: ElementRef<HTMLCanvasElement>;

  sidebarOpen = signal(false);
  displayUserName = this.authService.user()?.fullName || 'ولي الأمر';

  children = signal<ParentDashboardChild[]>([]);
  dashboardData = signal<ParentDashboardData | null>(null);
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);

  totalChildren = signal(0);
  activeChildren = signal(0);
  inactiveChildren = signal(0);

  selectedChild = signal<ParentChildStats | null>(null);
  selectedWeekDetail = signal<WeeklyPerformance | null>(null);
  selectedTerm = signal<number | null>(null);
  readonly terms = [
    { value: null, label: 'الترم الحالي' },
    { value: 1, label: 'الترم الأول' },
    { value: 2, label: 'الترم الثاني' },
  ];

  private performanceChart: Chart | null = null;
  private absencesChart: Chart | null = null;
  private weeklyChart: Chart | null = null;

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

        this.parentDashboardService.getDashboard(this.selectedTerm() ?? undefined).subscribe({
          next: (dashRes) => {
            const dashData = dashRes?.data ?? null;
            this.dashboardData.set(dashData);
            if (dashData?.children?.length) {
              this.selectedChild.set(dashData.children[0]);
            }
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

  selectChild(child: ParentChildStats): void {
    this.selectedChild.set(child);
    setTimeout(() => this.buildCharts(), 100);
  }

  getGradeClass(score: number, max: number): string {
    const pct = max > 0 ? (score / max) * 100 : 0;
    if (pct >= 85) return 'grade-excellent';
    if (pct >= 70) return 'grade-good';
    if (pct >= 50) return 'grade-pass';
    return 'grade-fail';
  }

  getPerformanceColor(pct: number): string {
    if (pct >= 85) return '#10b981';
    if (pct >= 70) return '#f59e0b';
    if (pct >= 50) return '#f97316';
    return '#ef4444';
  }

  private buildCharts() {
    const d = this.dashboardData();
    if (!d || !d.children.length) return;
    this.buildPerformanceChart(d.children);
    this.buildAbsencesChart(d.children);
    this.buildWeeklyChart();
  }

  private buildPerformanceChart(children: { name: string; performance: number }[]) {
    if (!this.performanceChartCanvas) return;
    const ctx = this.performanceChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;
    if (this.performanceChart) this.performanceChart.destroy();

    // Build segments: each child gets two slices (score + remainder)
    const labels: string[] = [];
    const data: number[] = [];
    const colors: string[] = [];
    const mainColors = ['#6366f1', '#10b981', '#f59e0b', '#ec4899', '#06b6d4'];

    children.forEach((child, i) => {
      const perf = child.performance || 0;
      const rem = Math.max(100 - perf, 0);
      labels.push(child.name, '');
      data.push(perf, rem);
      const color = mainColors[i % mainColors.length];
      colors.push(color, '#ef4444'); // remainder in medium red
    });

    this.performanceChart = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels,
        datasets: [{
          data,
          backgroundColor: colors,
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
            labels: {
              usePointStyle: true,
              padding: 14,
              font: { size: 12 },
              filter: (item: any) => item.text !== ''
            }
          },
          tooltip: {
            rtl: true,
            backgroundColor: '#1e1b4b',
            padding: 10,
            cornerRadius: 8,
            callbacks: {
              label: (ctx: any) => {
                if (ctx.label === '') return;
                return `${ctx.label}: ${ctx.parsed}%`;
              }
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

  private buildWeeklyChart() {
    if (!this.weeklyChartCanvas) return;
    const child = this.selectedChild();
    if (!child || !child.weeklyPerformances?.length) return;

    const ctx = this.weeklyChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;
    if (this.weeklyChart) this.weeklyChart.destroy();

    const weeks = child.weeklyPerformances.map(w => w.periodName);
    const scores = child.weeklyPerformances.map(w =>
      w.maxScore > 0 ? Math.round((w.avgScore / w.maxScore) * 100) : 0
    );

    const gradient = ctx.createLinearGradient(0, 0, 0, 250);
    gradient.addColorStop(0, 'rgba(99, 102, 241, 0.3)');
    gradient.addColorStop(1, 'rgba(99, 102, 241, 0.01)');

    this.weeklyChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: weeks,
        datasets: [{
          label: 'نسبة الأداء %',
          data: scores,
          borderColor: '#6366f1',
          backgroundColor: gradient,
          fill: true,
          tension: 0.3,
          pointBackgroundColor: '#6366f1',
          pointBorderColor: '#fff',
          pointBorderWidth: 2,
          pointRadius: 4,
          pointHoverRadius: 6,
          borderWidth: 2,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        onClick: (_: any, elements: any[]) => {
          if (elements.length > 0) {
            const idx = elements[0].index;
            this.selectedWeekDetail.set(child.weeklyPerformances[idx]);
          }
        },
        plugins: {
          legend: { display: false },
          tooltip: {
            rtl: true,
            backgroundColor: '#1e1b4b',
            padding: 12,
            cornerRadius: 8,
            callbacks: {
              title: (ctx: any) => ctx[0]?.label || '',
              label: (ctx: any) => {
                const i = ctx.dataIndex;
                const perf = child.weeklyPerformances[i];
                return [
                  `المجموع: ${perf.totalScore}/${perf.totalMaxScore}`,
                  `النسبة: ${ctx.parsed.y}%`
                ];
              }
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            max: 100,
            grid: { color: 'rgba(0,0,0,0.05)' },
            ticks: { callback: (v: any) => v + '%' }
          },
          x: { grid: { display: false } }
        }
      },
      plugins: [{
        id: 'pctLabels',
        afterDraw(chart: any) {
          const ctx2 = chart.ctx;
          chart.data.datasets.forEach((dataset: any, i: number) => {
            const meta = chart.getDatasetMeta(i);
            meta.data.forEach((bar: any, index: number) => {
              const val = dataset.data[index];
              ctx2.fillStyle = '#6366f1';
              ctx2.font = 'bold 11px sans-serif';
              ctx2.textAlign = 'center';
              ctx2.fillText(val + '%', bar.x, bar.y - 10);
            });
          });
        }
      }]
    });
  }

  childStats(name: string) {
    return this.dashboardData()?.children.find(c => c.name === name);
  }

  switchTerm(termValue: number | null): void {
    this.selectedTerm.set(termValue);
    this.loadData();
  }

  closeWeekDetail() {
    this.selectedWeekDetail.set(null);
  }
}

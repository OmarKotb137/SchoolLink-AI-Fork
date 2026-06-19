import { Component, signal, AfterViewInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-reports-academic',
  imports: [Sidebar],
  templateUrl: './reports-academic.html',
  styleUrl: './reports-academic.css',
})
export class ReportsAcademic implements AfterViewInit, OnDestroy {
  @ViewChild('weeklyChart') weeklyChartCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('monthlyChart') monthlyChartCanvas!: ElementRef<HTMLCanvasElement>;
  chart1: Chart | null = null;
  chart2: Chart | null = null;
  sidebarOpen = signal(false);

  ngAfterViewInit() {
    this.createWeeklyChart();
    this.createMonthlyChart();
  }

  ngOnDestroy() {
    this.chart1?.destroy();
    this.chart2?.destroy();
  }

  private createWeeklyChart() {
    if (!this.weeklyChartCanvas) return;
    const ctx = this.weeklyChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.chart1 = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: ['الأسبوع 1', 'الأسبوع 2', 'الأسبوع 3', 'الأسبوع 4'],
        datasets: [
          { label: 'أحمد علي', data: [28, 30, 29, 27], backgroundColor: '#4F46E5', borderRadius: 4 },
          { label: 'سارة محمود', data: [32, 35, 33, 34], backgroundColor: '#10B981', borderRadius: 4 },
          { label: 'يوسف حسن', data: [22, 24, 20, 25], backgroundColor: '#F59E0B', borderRadius: 4 },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'top', labels: { usePointStyle: true, font: { size: 11 } } },
        },
        scales: {
          y: { beginAtZero: true, max: 40, ticks: { stepSize: 10 } },
        },
      },
    });
  }

  private createMonthlyChart() {
    if (!this.monthlyChartCanvas) return;
    const ctx = this.monthlyChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.chart2 = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: ['متوسط فبراير', 'متوسط مارس', 'متوسط أبريل', 'الاختبار الشهري'],
        datasets: [
          { label: 'أحمد علي', data: [28.5, 31, 33, 13], backgroundColor: '#4F46E5', borderRadius: 4 },
          { label: 'سارة محمود', data: [33.5, 35, 36, 14], backgroundColor: '#10B981', borderRadius: 4 },
          { label: 'يوسف حسن', data: [22.75, 24, 26, 10], backgroundColor: '#F59E0B', borderRadius: 4 },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'top', labels: { usePointStyle: true, font: { size: 11 } } },
        },
        scales: {
          y: { beginAtZero: true, max: 40, ticks: { stepSize: 10 } },
        },
      },
    });
  }
}



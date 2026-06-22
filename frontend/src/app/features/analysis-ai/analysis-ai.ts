import { Component, signal, AfterViewInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-analysis-ai',
  imports: [Sidebar],
  templateUrl: './analysis-ai.html',
  styleUrl: './analysis-ai.css',
})
export class AnalysisAi implements AfterViewInit, OnDestroy {
  @ViewChild('trendChart') trendChartCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('subjectChart') subjectChartCanvas!: ElementRef<HTMLCanvasElement>;
  chart1: Chart | null = null;
  chart2: Chart | null = null;
  sidebarOpen = signal(false);

  ngAfterViewInit() {
    this.createTrendChart();
    this.createSubjectChart();
  }

  ngOnDestroy() {
    this.chart1?.destroy();
    this.chart2?.destroy();
  }

  private createTrendChart() {
    if (!this.trendChartCanvas) return;
    const ctx = this.trendChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.chart1 = new Chart(ctx, {
      type: 'line',
      data: {
        labels: ['يناير', 'فبراير', 'مارس'],
        datasets: [
          {
            label: 'الرياضيات',
            data: [78, 82, 85],
            borderColor: '#4F46E5',
            backgroundColor: 'rgba(79, 70, 229, 0.1)',
            fill: true,
            tension: 0.4,
            pointBackgroundColor: '#4F46E5',
          },
          {
            label: 'العلوم',
            data: [72, 68, 72],
            borderColor: '#10B981',
            backgroundColor: 'rgba(16, 185, 129, 0.1)',
            fill: true,
            tension: 0.4,
            pointBackgroundColor: '#10B981',
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'top', labels: { usePointStyle: true, font: { size: 11 } } },
        },
        scales: {
          y: { beginAtZero: true, max: 100, ticks: { stepSize: 20 } },
        },
      },
    });
  }

  private createSubjectChart() {
    if (!this.subjectChartCanvas) return;
    const ctx = this.subjectChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.chart2 = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: ['الرياضيات', 'الفيزياء', 'البرمجة', 'الكيمياء', 'اللغات'],
        datasets: [
          {
            label: 'نسبة الأداء',
            data: [85, 72, 92, 65, 88],
            backgroundColor: [
              '#4F46E5', '#10B981', '#8B5CF6', '#F59E0B', '#3B82F6',
            ],
            borderRadius: 6,
            barThickness: 48,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
        },
        scales: {
          y: { beginAtZero: true, max: 100, ticks: { stepSize: 20, callback: (v) => v + '%' } },
        },
      },
    });
  }
}



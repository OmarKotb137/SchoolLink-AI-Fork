import { Component, signal, AfterViewInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-reports-training',
  imports: [Sidebar],
  templateUrl: './reports-training.html',
  styleUrl: './reports-training.css',
})
export class ReportsTraining implements AfterViewInit, OnDestroy {
  @ViewChild('assignmentChart') assignmentChartCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('examChart') examChartCanvas!: ElementRef<HTMLCanvasElement>;
  chart1: Chart | null = null;
  chart2: Chart | null = null;
  sidebarOpen = signal(false);

  ngAfterViewInit() {
    this.createAssignmentChart();
    this.createExamChart();
  }

  ngOnDestroy() {
    this.chart1?.destroy();
    this.chart2?.destroy();
  }

  private createAssignmentChart() {
    if (!this.assignmentChartCanvas) return;
    const ctx = this.assignmentChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.chart1 = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: ['تم التسليم', 'متأخر', 'لم يسلم'],
        datasets: [{
          data: [2, 1, 1],
          backgroundColor: ['#10B981', '#F59E0B', '#EF4444'],
          borderWidth: 0,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '70%',
        plugins: {
          legend: { position: 'bottom', labels: { usePointStyle: true, font: { size: 11 } } },
        },
      },
    });
  }

  private createExamChart() {
    if (!this.examChartCanvas) return;
    const ctx = this.examChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.chart2 = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: ['الرياضيات', 'العلوم', 'العربية', 'إنجليزي'],
        datasets: [
          { label: 'الدرجة', data: [16, 13, 18, 5], backgroundColor: '#4F46E5', borderRadius: 4 },
          { label: 'الدرجة القصوى', data: [20, 15, 25, 10], backgroundColor: '#E5E7EB', borderRadius: 4 },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'top', labels: { usePointStyle: true, font: { size: 11 } } },
        },
        scales: {
          y: { beginAtZero: true, ticks: { stepSize: 5 } },
        },
      },
    });
  }
}



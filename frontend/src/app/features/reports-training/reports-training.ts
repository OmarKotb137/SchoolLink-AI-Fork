import {
  Component,
  signal,
  AfterViewInit,
  OnDestroy,
  ElementRef,
  ViewChild,
  inject,
  OnInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { HttpClient } from '@angular/common/http';
import { buildApiUrl } from '../../core/utils/api-url';
import { Chart, registerables } from 'chart.js';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

Chart.register(...registerables);

interface ClassItem {
  id: number;
  name: string;
  gradeLevelName?: string;
  gradeLevelId?: number;
}

interface AssignmentItem {
  id: number;
  title: string;
  subject: string | null;
  class: string | null;
  deadline: string;
  maxScore: number;
  submitted: number | null;
  total: number | null;
  avgScore?: number;
  status: string;
}

interface ExamItem {
  id: number;
  title: string;
  subject?: string;
  questionsCount: number;
  publishDate?: string;
  durationMinutes?: number;
  totalAttempts?: number;
  avgScore?: number;
  maxScore?: number;
  passRate?: number;
}

@Component({
  selector: 'app-reports-training',
  imports: [Sidebar, CommonModule, FormsModule],
  templateUrl: './reports-training.html',
  styleUrl: './reports-training.css',
})
export class ReportsTraining implements AfterViewInit, OnDestroy, OnInit {
  @ViewChild('assignmentChart') assignmentChartCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('examChart') examChartCanvas!: ElementRef<HTMLCanvasElement>;

  private http = inject(HttpClient);
  private base = buildApiUrl();

  sidebarOpen = signal(false);
  chart1: Chart | null = null;
  chart2: Chart | null = null;

  // State
  loading = signal(false);
  classes: ClassItem[] = [];
  selectedClassId: number | null = null;
  selectedTerm: number = 0; // 0 = all

  // Grade-level aggregation
  gradeLevelMode = false;
  gradeLevels: { id: number; name: string }[] = [];
  selectedGradeLevelId: number | null = null;

  // Data
  assignments: AssignmentItem[] = [];
  exams: ExamItem[] = [];
  currentAcademicYearId: number | null = null;

  // Stats
  assignmentSubmittedCount = 0;
  assignmentLateCount = 0;
  assignmentMissingCount = 0;
  assignmentAvgScore = 0;
  examAvgScore = 0;
  examPassRate = 0;
  totalExamAttempts = 0;

  ngOnInit() {
    this.loadInitialData();
  }

  ngAfterViewInit() {
    // charts created after data
  }

  ngOnDestroy() {
    this.chart1?.destroy();
    this.chart2?.destroy();
  }

  loadInitialData() {
    this.loading.set(true);
    forkJoin({
      yearRes: this.http.get<any>(`${this.base}/academic-years/current`).pipe(catchError(() => of({ data: null }))),
      classRes: this.http.get<any>(`${this.base}/class-management`).pipe(catchError(() => of([])))
    }).subscribe({
      next: ({ yearRes, classRes }) => {
        if (yearRes?.data) this.currentAcademicYearId = yearRes.data.id;
        const raw = Array.isArray(classRes) ? classRes : (classRes?.data ?? []);
        this.classes = raw;

        // Build distinct grade levels from classes
        const glMap = new Map<number, string>();
        for (const c of raw) {
          if (c.gradeLevelId && c.gradeLevelName) {
            glMap.set(c.gradeLevelId, c.gradeLevelName);
          }
        }
        this.gradeLevels = Array.from(glMap.entries()).map(([id, name]) => ({ id, name }));

        if (this.classes.length > 0) {
          this.selectedClassId = this.classes[0].id;
          if (this.gradeLevels.length > 0) {
            this.selectedGradeLevelId = this.gradeLevels[0].id;
          }
          this.loadReport();
        } else {
          this.loading.set(false);
        }
      },
      error: () => this.loading.set(false)
    });
  }

  loadReport() {
    this.loading.set(true);
    this.assignments = [];
    this.exams = [];

    const yearId = this.currentAcademicYearId;

    forkJoin({
      assignRes: this.http.get<any>(
        `${this.base}/assignment-manager?pageSize=100${yearId ? '&academicYearId=' + yearId : ''}`
      ).pipe(catchError(() => of({ data: { items: [] } }))),
      examRes: this.http.get<any>(
        `${this.base}/exam-manager${yearId ? '?academicYearId=' + yearId : ''}`
      ).pipe(catchError(() => of({ data: [] }))),
      statsRes: this.http.get<any>(
        `${this.base}/assignment-manager/stats${yearId ? '?academicYearId=' + yearId : ''}`
      ).pipe(catchError(() => of({ data: null })))
    }).subscribe({
      next: ({ assignRes, examRes, statsRes }) => {
        // Assignments
        const rawAssign = assignRes?.data?.items ?? assignRes?.data ?? [];
        this.assignments = Array.isArray(rawAssign) ? rawAssign : [];

        // Filter by class if selected (skip in grade-level mode)
        if (this.selectedClassId && !this.gradeLevelMode) {
          const cls = this.classes.find(c => c.id === this.selectedClassId);
          if (cls) {
            const clsName = cls.gradeLevelName ? `${cls.gradeLevelName} - ${cls.name}` : cls.name;
            this.assignments = this.assignments.filter(a =>
              !a.class || a.class.toLowerCase().includes(cls.name.toLowerCase()) ||
              a.class.toLowerCase().includes(clsName.toLowerCase())
            );
          }
        }

        // Exams
        const rawExams = examRes?.data?.items ?? examRes?.data ?? [];
        let mappedExams = Array.isArray(rawExams) ? rawExams : [];
        if (this.selectedClassId && !this.gradeLevelMode) {
          const cls = this.classes.find(c => c.id === this.selectedClassId);
          if (cls) {
            const clsName = cls.gradeLevelName ? `${cls.gradeLevelName} - ${cls.name}` : cls.name;
            mappedExams = mappedExams.filter((e: any) =>
              !e.class || e.class.toLowerCase().includes(cls.name.toLowerCase()) ||
              e.class.toLowerCase().includes(clsName.toLowerCase())
            );
          }
        }

        this.exams = mappedExams.map((e: any) => ({
          id: e.id,
          title: e.name ?? 'امتحان',
          subject: e.subject,
          questionsCount: e.questionCount ?? 0,
          publishDate: e.date,
          durationMinutes: e.duration ?? 0,
          totalAttempts: e.submitted ?? 0,
          avgScore: e.avgScore ?? 0,
          maxScore: 100,
          passRate: e.avgScore ? Math.round(e.avgScore) : 0
        }));

        this.computeStats(statsRes?.data);
        this.buildCharts();
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  computeStats(statsData: any) {
    // Assignment stats
    const submitted = this.assignments.filter(a => a.status === 'active' || a.status === 'closed' || (a.submitted ?? 0) > 0);
    this.assignmentSubmittedCount = this.assignments.reduce((s, a) => s + (a.submitted ?? 0), 0);
    this.assignmentMissingCount = this.assignments.reduce((s, a) => s + Math.max(0, (a.total ?? 0) - (a.submitted ?? 0)), 0);
    this.assignmentLateCount = 0;

    // Assignment avg score
    const withScore = this.assignments.filter(a => (a.avgScore ?? 0) > 0);
    this.assignmentAvgScore = withScore.length > 0
      ? Math.round(withScore.reduce((s, a) => s + (a.avgScore ?? 0), 0) / withScore.length)
      : 0;

    // Exam stats
    this.totalExamAttempts = this.exams.reduce((s, e) => s + (e.totalAttempts ?? 0), 0);
    const examsWithScore = this.exams.filter(e => (e.avgScore ?? 0) > 0);
    this.examAvgScore = examsWithScore.length > 0
      ? Math.round(examsWithScore.reduce((s, e) => s + (e.avgScore ?? 0), 0) / examsWithScore.length)
      : 0;
    this.examPassRate = this.exams.length > 0
      ? Math.round(this.exams.reduce((s, e) => s + (e.passRate ?? 0), 0) / this.exams.length)
      : 0;

    // Override with server stats if available
    if (statsData) {
      if (statsData.avgDelivery != null) {
        this.assignmentAvgScore = Math.round(statsData.avgDelivery);
      }
    }
  }

  buildCharts() {
    setTimeout(() => {
      this.chart1?.destroy();
      this.chart2?.destroy();
      this.createAssignmentChart();
      this.createExamChart();
    }, 100);
  }

  private createAssignmentChart() {
    if (!this.assignmentChartCanvas) return;
    const ctx = this.assignmentChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    const submitted = this.assignmentSubmittedCount;
    const missing = this.assignmentMissingCount;
    const hasData = submitted > 0 || missing > 0;

    this.chart1 = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: ['تم التسليم', 'لم يسلم'],
        datasets: [{
          data: hasData
            ? [Math.max(submitted, 0), Math.max(missing, 0)]
            : [1],
          backgroundColor: hasData
            ? ['#10B981', '#EF4444']
            : ['#E5E7EB'],
          borderWidth: 0,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '72%',
        plugins: {
          legend: {
            position: 'bottom',
            labels: { usePointStyle: true, font: { size: 11 } },
          },
        },
      },
    });
  }

  private createExamChart() {
    if (!this.examChartCanvas) return;
    const ctx = this.examChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    const labels = this.exams.slice(0, 10).map(e => e.title);
    const scores = this.exams.slice(0, 10).map(e => e.avgScore ?? 0);

    if (labels.length === 0) {
      labels.push('لا توجد بيانات');
      scores.push(0);
    }

    this.chart2 = new Chart(ctx, {
      type: 'bar',
      data: {
        labels,
        datasets: [
          {
            label: 'متوسط الدرجات',
            data: scores,
            backgroundColor: scores.map(s => s >= 60 ? '#10B981' : '#EF4444'),
            borderRadius: 6,
            borderSkipped: false,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx) => `${ctx.parsed.y}%`,
            },
          },
        },
        scales: {
          y: {
            beginAtZero: true,
            max: 100,
            ticks: { stepSize: 20, callback: (v) => v + '%' },
          },
        },
      },
    });
  }

  onClassChange() { this.loadReport(); }
  onTermChange() { this.loadReport(); }

  getAssignmentStatus(a: AssignmentItem): { label: string; cls: string } {
    if (a.status === 'active') return { label: 'نشط', cls: 'status-active' };
    if (a.status === 'closed') return { label: 'مغلق', cls: 'status-closed' };
    if (a.status === 'draft') return { label: 'مسودة', cls: 'status-draft' };
    if (a.status === 'open') return { label: 'مفتوح', cls: 'status-active' };
    return { label: '—', cls: 'status-default' };
  }

  getSubmissionRate(a: AssignmentItem): number {
    const total = a.total ?? 0;
    if (total === 0) return 0;
    return Math.round(((a.submitted ?? 0) / total) * 100);
  }

  getExamScorePct(e: ExamItem): number {
    if (!e.maxScore || e.maxScore === 0) return 0;
    return Math.round(((e.avgScore ?? 0) / e.maxScore) * 100);
  }

  getSubmissionDisplay(a: AssignmentItem): string {
    const sub = a.submitted ?? 0;
    const tot = a.total ?? 0;
    return tot > 0 ? `${sub} / ${tot}` : `${sub}`;
  }

  get selectedClassName(): string {
    if (this.gradeLevelMode && this.selectedGradeLevelId) {
      const gl = this.gradeLevels.find(g => g.id === this.selectedGradeLevelId);
      return gl ? gl.name : '';
    }
    const c = this.classes.find(c => c.id === this.selectedClassId);
    return c ? (c.gradeLevelName ? `${c.gradeLevelName} - ${c.name}` : c.name) : 'كل الفصول';
  }

  get assignmentDeliveryRate(): number {
    const total = this.assignments.reduce((s, a) => s + (a.total ?? 0), 0);
    if (total === 0) return 0;
    return Math.round((this.assignmentSubmittedCount / total) * 100);
  }
}

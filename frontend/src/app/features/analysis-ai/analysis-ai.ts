import { CommonModule } from '@angular/common';
import { Component, signal, computed, inject, AfterViewInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Chart, registerables } from 'chart.js';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import {
  AnalysisAiService,
  TeacherGrowthCard,
  TeacherGrowthDashboard,
  TeacherGrowthStudentPage,
  StudentGrowthWeek,
  StudentGrowthRanking,
  StudentExamSummary,
  StudentFinalGradeSummary,
} from './analysis-ai.service';

Chart.register(...registerables);

@Component({
  selector: 'app-analysis-ai',
  imports: [CommonModule, FormsModule, Sidebar],
  templateUrl: './analysis-ai.html',
  styleUrl: './analysis-ai.css',
})
export class AnalysisAi implements AfterViewInit, OnDestroy {
  @ViewChild('growthChart') growthChartCanvas?: ElementRef<HTMLCanvasElement>;
  @ViewChild('rankingChart') rankingChartCanvas?: ElementRef<HTMLCanvasElement>;

  private service = inject(AnalysisAiService);

  sidebarOpen = signal(false);
  loading = signal(true);
  detailsLoading = signal(false);
  error = signal<string | null>(null);
  selectedTerm = signal<number>(0); // 0 = auto-detect current term from backend
  selectedCard = signal<TeacherGrowthCard | null>(null);
  studentPage = signal<TeacherGrowthStudentPage | null>(null);
  selectedStudentWeeks = signal<StudentGrowthWeek[] | null>(null);
  selectedStudentName = signal<string>('');
  weeksLoading = signal(false);
  selectedStudentFinalGrades = signal<StudentFinalGradeSummary | null>(null);
  finalGradeLoading = signal(false);
  page = signal(1);
  pageSize = 20;
  dashboard = signal<TeacherGrowthDashboard | null>(null);

  growthChart: Chart | null = null;
  rankingChart: Chart | null = null;
  chartsReady = false;

  // Shared colour palette for teachers
  readonly palette = [
    '#6366f1', '#f43f5e', '#10b981', '#f59e0b', '#8b5cf6',
    '#ec4899', '#14b8a6', '#f97316', '#06b6d4', '#84cc16',
    '#e11d48', '#0ea5e9', '#a855f7', '#22c55e', '#d946ef',
    '#fb923c', '#38bdf8', '#4ade80', '#f87171', '#818cf8',
  ];

  teachers = computed(() => this.dashboard()?.teachers ?? []);
  topTeachers = computed(() => this.teachers().slice(0, 5));
  teachersWeeklyTrend = computed(() => this.dashboard()?.teachersWeeklyTrend ?? []);
  watchTeachers = computed(() => this.teachers().filter(t => t.riskLevel !== 'healthy').slice(0, 5));

  /** The teacher currently shown on the growth chart */
  selectedTeacherTrendId = signal<number | null>(null);
  /** Full trend data for the selected teacher */
  selectedTeacherTrend = computed(() => {
    const id = this.selectedTeacherTrendId();
    if (id == null) return null;
    return this.teachersWeeklyTrend().find(t => t.teacherId === id) ?? null;
  });
  /** Index for colour lookup */
  selectedTeacherTrendIndex = computed(() => {
    const id = this.selectedTeacherTrendId();
    if (id == null) return 0;
    return this.teachersWeeklyTrend().findIndex(t => t.teacherId === id);
  });
  aggregatedWatchTeachers = computed(() => {
    const all = this.teachers();
    const map = new Map<number, { card: TeacherGrowthCard; totalDeclined: number; totalEvaluated: number; totalImproved: number; worstAvgChange: number }>();
    for (const t of all) {
      if (t.riskLevel === 'healthy') continue;
      const existing = map.get(t.teacherId);
      if (existing) {
        existing.totalDeclined += t.declinedCount;
        existing.totalEvaluated += t.evaluatedStudentsCount;
        existing.totalImproved += t.improvedCount;
        // Track the worst (most negative) average change
        if (t.averageChange < existing.worstAvgChange) existing.worstAvgChange = t.averageChange;
      } else {
        map.set(t.teacherId, { card: t, totalDeclined: t.declinedCount, totalEvaluated: t.evaluatedStudentsCount, totalImproved: t.improvedCount, worstAvgChange: t.averageChange });
      }
    }
    return Array.from(map.values())
      .sort((a, b) => b.totalDeclined - a.totalDeclined)
      .slice(0, 5)
      .map(item => ({
        ...item.card,
        declinedCount: item.totalDeclined,
        improvedCount: item.totalImproved,
        evaluatedStudentsCount: item.totalEvaluated,
        studentsCount: item.totalEvaluated,
        averageChange: item.worstAvgChange,
        subjectName: 'جميع المواد',
        className: '',
        gradeLevelName: '',
        subjectId: 0,
        classId: 0,
        improvedStudentsRate: item.totalEvaluated > 0 ? item.totalImproved / item.totalEvaluated * 100 : 0,
        declinedStudentsRate: item.totalEvaluated > 0 ? item.totalDeclined / item.totalEvaluated * 100 : 0,
      }));
  });
  bestTeacher = computed(() => this.teachers()[0] ?? null);
  isAggregatedView = computed(() => this.selectedCard()?.classId === 0);
  selectedStudents = computed(() => this.studentPage()?.students.items ?? []);
  totalPages = computed(() => this.studentPage()?.students.totalPages ?? 0);
  weeksFirstAvg = computed(() => {
    const weeks = this.selectedStudentWeeks();
    if (!weeks || weeks.length === 0) return 0;
    const first = weeks.filter(w => w.isFirstHalf);
    return first.length > 0 ? first.reduce((s, w) => s + w.percentage, 0) / first.length : 0;
  });
  weeksSecondAvg = computed(() => {
    const weeks = this.selectedStudentWeeks();
    if (!weeks || weeks.length === 0) return 0;
    const second = weeks.filter(w => !w.isFirstHalf);
    return second.length > 0 ? second.reduce((s, w) => s + w.percentage, 0) / second.length : 0;
  });

  // ── Top / Bottom Student Rankings ──
  rankings = signal<StudentGrowthRanking | null>(null);
  topImprovedStudents = computed(() => this.rankings()?.topImproved ?? []);
  topDeclinedStudents = computed(() => this.rankings()?.topDeclined ?? []);
  topEvalStudents = computed(() => this.rankings()?.topEvaluationStudents ?? []);
  topMonthlyExamStudents = computed(() => this.rankings()?.topMonthlyExamStudents ?? []);
  topFinalExamStudents = computed(() => this.rankings()?.topFinalExamStudents ?? []);
  rankingsLoading = signal(false);
  showRankingsModal = signal(false);
  rankingsTab = signal(0); // 0 = rankings, 1 = top evaluations

  // ── Student Monthly Exam Summary ──
  selectedStudentExams = signal<StudentExamSummary | null>(null);
  examLoading = signal(false);
  selectedExamStudentName = signal<string>('');

  ngAfterViewInit() {
    this.chartsReady = true;
    this.loadDashboard();
  }

  ngOnDestroy() {
    this.growthChart?.destroy();
    this.rankingChart?.destroy();
  }

  loadDashboard() {
    this.loading.set(true);
    this.error.set(null);
    const term = this.selectedTerm() || undefined;

    forkJoin([
      this.service.getTeacherGrowthOverview(term),
      this.service.getTeacherGrowthTeachers(term),
    ]).subscribe({
      next: ([overviewRes, teachersRes]) => {
        if (overviewRes.isSuccess && overviewRes.data && teachersRes.isSuccess && teachersRes.data) {
          const overview = overviewRes.data;
          const teachers = teachersRes.data;
          const dashboard: TeacherGrowthDashboard = {
            academicYearId: overview.academicYearId,
            academicYearName: overview.academicYearName,
            term: overview.term,
            evaluatedWeeks: overview.evaluatedWeeks,
            totalConfiguredWeeks: overview.totalConfiguredWeeks,
            teachersCount: overview.teachersCount,
            schoolGrowthRate: overview.schoolGrowthRate,
            schoolAverageChange: overview.schoolAverageChange,
            improvedStudentsRate: overview.improvedStudentsRate,
            declinedStudentsRate: overview.declinedStudentsRate,
            totalImprovedCount: overview.totalImprovedCount,
            totalDeclinedCount: overview.totalDeclinedCount,
            totalEvaluatedCount: overview.totalEvaluatedCount,
            teachers: teachers.teachers,
            weeklyTrend: overview.weeklyTrend,
            teachersWeeklyTrend: overview.teachersWeeklyTrend,
            signals: overview.signals,
          };
          this.dashboard.set(dashboard);
          if (overview.term) {
            this.selectedTerm.set(overview.term);
          }
          // Select the first teacher trend as default
          const trends = this.teachersWeeklyTrend();
          this.selectedTeacherTrendId.set(trends.length > 0 ? trends[0].teacherId : null);
          this.renderCharts();
        } else {
          this.error.set(overviewRes.message || teachersRes.message || 'تعذر تحميل مؤشرات التحسن.');
        }
        this.loading.set(false);
        this.loadRankings();
      },
      error: () => {
        this.error.set('تعذر الاتصال بخدمة التحليلات.');
        this.loading.set(false);
      },
    });
  }

  changeTerm(term: number) {
    this.selectedTerm.set(term);
    this.closeDetails();
    this.loadDashboard();
  }

  openDetails(card: TeacherGrowthCard, page = 1) {
    this.page.set(page);
    this.detailsLoading.set(true);
    const term = this.selectedTerm() || undefined;

    // Pass only teacherId to get aggregated view across all the teacher's classes/subjects
    this.service.getTeacherGrowthStudents(card.teacherId, card.classId, card.subjectId, term, page, this.pageSize).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.studentPage.set(res.data);
          // Use the fresh summary from the detail API (not the stale main-list card)
          this.selectedCard.set(res.data.summary);
        }
        this.detailsLoading.set(false);
      },
      error: () => this.detailsLoading.set(false),
    });
  }

  closeDetails() {
    this.selectedCard.set(null);
    this.studentPage.set(null);
    this.page.set(1);
  }

  nextPage() {
    const card = this.selectedCard();
    if (!card || this.page() >= this.totalPages()) return;
    this.openDetails(card, this.page() + 1);
  }

  previousPage() {
    const card = this.selectedCard();
    if (!card || this.page() <= 1) return;
    this.openDetails(card, this.page() - 1);
  }

  openStudentWeeks(student: { studentId: number; studentName: string }) {
    const card = this.selectedCard();
    if (!card) return;
    this.selectedStudentName.set(student.studentName);
    this.weeksLoading.set(true);
    const term = this.selectedTerm() || undefined;
    this.service.getStudentWeeks(student.studentId, undefined, undefined, card.teacherId, term).subscribe({
      next: (res) => {
        this.selectedStudentWeeks.set(res.isSuccess && res.data ? res.data : []);
        this.weeksLoading.set(false);
      },
      error: () => {
        this.selectedStudentWeeks.set([]);
        this.weeksLoading.set(false);
      },
    });
  }

  openRankingStudentWeeks(studentId: number, studentName: string, subjectId: number) {
    this.selectedStudentName.set(studentName);
    this.weeksLoading.set(true);
    const term = this.selectedTerm() || undefined;
    this.service.getStudentWeeks(studentId, undefined, subjectId || undefined, undefined, term).subscribe({
      next: (res) => {
        this.selectedStudentWeeks.set(res.isSuccess && res.data ? res.data : []);
        this.weeksLoading.set(false);
      },
      error: () => {
        this.selectedStudentWeeks.set([]);
        this.weeksLoading.set(false);
      },
    });
  }

  closeStudentWeeks() {
    this.selectedStudentWeeks.set(null);
    this.selectedStudentName.set('');
  }

  loadRankings() {
    this.rankingsLoading.set(true);
    const term = this.selectedTerm() || undefined;
    this.service.getStudentGrowthRankings(term).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.rankings.set(res.data);
        }
        this.rankingsLoading.set(false);
      },
      error: () => this.rankingsLoading.set(false),
    });
  }

  openStudentExams(studentId: number, studentName: string) {
    this.selectedExamStudentName.set(studentName);
    this.examLoading.set(true);
    const term = this.selectedTerm() || undefined;
    this.service.getStudentExamSummary(studentId, term).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.selectedStudentExams.set(res.data);
        }
        this.examLoading.set(false);
      },
      error: () => this.examLoading.set(false),
    });
  }

  closeStudentExams() {
    this.selectedStudentExams.set(null);
    this.selectedExamStudentName.set('');
  }

  openStudentFinalGrades(studentId: number, studentName: string) {
    this.selectedStudentName.set(studentName);
    this.finalGradeLoading.set(true);
    const term = this.selectedTerm() || undefined;
    this.service.getStudentFinalGrades(studentId, term).subscribe({
      next: (res) => {
        this.selectedStudentFinalGrades.set(res.isSuccess && res.data ? res.data : null);
        this.finalGradeLoading.set(false);
      },
      error: () => this.finalGradeLoading.set(false),
    });
  }

  closeStudentFinalGrades() {
    this.selectedStudentFinalGrades.set(null);
    this.selectedStudentName.set('');
  }

  openRankingsModal() {
    this.showRankingsModal.set(true);
    this.rankingsTab.set(0);
  }

  closeRankingsModal() {
    this.showRankingsModal.set(false);
  }

  switchRankingsTab(tab: number) {
    this.rankingsTab.set(tab);
  }

  metricTone(value: number): string {
    if (value >= 8) return 'positive';
    if (value <= -4) return 'negative';
    return 'neutral';
  }

  statusLabel(status: string): string {
    if (status === 'improved') return 'تحسن';
    if (status === 'declined') return 'انخفض';
    return 'ثابت';
  }

  statusIcon(status: string): string {
    if (status === 'improved') return 'trending_up';
    if (status === 'declined') return 'trending_down';
    return 'drag_handle';
  }

  riskLabel(risk: string): string {
    if (risk === 'healthy') return 'ممتاز';
    if (risk === 'critical') return 'خطر';
    return 'متابعة';
  }

  private renderCharts() {
    if (!this.chartsReady) return;
    setTimeout(() => {
      this.renderGrowthChart();
      this.renderRankingChart();
    });
  }

  renderGrowthChart() {
    const canvas = this.growthChartCanvas?.nativeElement;
    const data = this.dashboard();
    if (!canvas || !data) return;
    this.growthChart?.destroy();

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const labels = data.weeklyTrend.map(w => w.label);
    const weekOrders = data.weeklyTrend.map(w => w.orderNum);
    const selectedId = this.selectedTeacherTrendId();
    const selected = selectedId != null
      ? data.teachersWeeklyTrend.find(t => t.teacherId === selectedId)
      : null;

    if (!selected) {
      this.growthChart = null;
      return;
    }

    // Y-range using this teacher + school average
    const allVals: number[] = selected.weeklyScores.map(s => s.averageScore);
    allVals.push(...data.weeklyTrend.map(w => w.averageScore));
    const mn = allVals.length ? Math.floor(Math.min(...allVals)) : 0;
    const mx = allVals.length ? Math.ceil(Math.max(...allVals)) : 100;
    const pad = Math.max(5, Math.round((mx - mn) * 0.18));
    const yMin = Math.max(0, mn - pad);
    const yMax = Math.min(100, mx + pad);

    const idx = data.teachersWeeklyTrend.indexOf(selected);
    const color = this.palette[idx % this.palette.length];
    const teacherVals = weekOrders.map(o => {
      const ws = selected.weeklyScores.find(s => s.orderNum === o);
      return ws ? ws.averageScore : null;
    });

    // Gradient fill under teacher line
    const grad = ctx.createLinearGradient(0, 0, 0, ctx.canvas.height * 0.5);
    grad.addColorStop(0, color + '50');
    grad.addColorStop(1, color + '05');

    this.growthChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: selected.teacherName,
            data: teacherVals,
            borderColor: color,
            backgroundColor: grad,
            pointBackgroundColor: color,
            pointBorderColor: '#fff',
            pointBorderWidth: 2,
            pointRadius: 6,
            pointHoverRadius: 10,
            pointHoverBorderWidth: 3,
            borderWidth: 4,
            hoverBorderWidth: 5,
            tension: 0.3,
            fill: true,
            order: 1,
          },
          {
            label: 'المتوسط العام',
            data: data.weeklyTrend.map(w => w.averageScore),
            borderColor: '#94a3b8',
            backgroundColor: 'rgba(148, 163, 184, 0.08)',
            pointBackgroundColor: '#94a3b8',
            pointBorderColor: '#fff',
            pointBorderWidth: 1.5,
            pointRadius: 3,
            pointHoverRadius: 6,
            borderWidth: 2,
            borderDash: [6, 3],
            tension: 0.3,
            fill: false,
            order: 0,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'nearest', intersect: false },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#0f172a',
            titleFont: { size: 13, weight: 'bold' },
            titleColor: '#f8fafc',
            bodyFont: { size: 12 },
            bodyColor: '#cbd5e1',
            padding: 12,
            cornerRadius: 8,
            callbacks: {
              title: items => items[0]?.label ?? '',
              label: item => {
                const v = item.parsed.y;
                if (v == null) return '';
                if (item.dataset.label === 'المتوسط العام') return `المتوسط العام: ${v.toFixed(1)}%`;
                return `${v.toFixed(1)}%`;
              },
            },
          },
        },
        scales: {
          x: {
            grid: { color: 'rgba(148,163,184,.08)' },
            ticks: { color: '#64748b', font: { size: 11, weight: 'bold' }, maxRotation: 40 },
          },
          y: {
            min: yMin, max: yMax,
            grid: { color: 'rgba(148,163,184,.12)' },
            ticks: {
              color: '#64748b',
              font: { size: 11, weight: 'bold' },
              callback: v => `${v}%`,
            },
          },
        },
      },
    });
  }

  private renderRankingChart() {
    const canvas = this.rankingChartCanvas?.nativeElement;
    const data = this.topTeachers();
    if (!canvas) return;
    this.rankingChart?.destroy();

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    this.rankingChart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: data.map(t => t.teacherName),
        datasets: [{
          label: 'عدد الطلاب المتحسنين',
          data: data.map(t => t.improvedCount),
          backgroundColor: ['#22c55e', '#14b8a6', '#3b82f6', '#8b5cf6', '#f59e0b'],
          borderRadius: 8,
          barThickness: 30,
        }],
      },
      options: {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#0f172a',
            titleFont: { size: 14, weight: 'bold' },
            titleColor: '#f8fafc',
            bodyFont: { size: 12 },
            bodyColor: '#cbd5e1',
            padding: 14,
            cornerRadius: 10,
            displayColors: false,
            callbacks: {
              title: ctx => {
                const idx = ctx[0]?.dataIndex;
                const t = idx != null ? data[idx] : null;
                return t ? t.teacherName : '';
              },
              beforeBody: ctx => {
                const idx = ctx[0]?.dataIndex;
                const t = idx != null ? data[idx] : null;
                return t ? `${t.subjectName} · ${t.gradeLevelName} ${t.className}` : '';
              },
              label: ctx => {
                return ctx.parsed.x != null ? `🟢 ${ctx.parsed.x} طالب تحسنوا` : '';
              },
            },
          },
        },
        scales: {
          x: {
            grid: { color: 'rgba(148, 163, 184, .12)' },
            ticks: { color: '#64748b', font: { size: 11, weight: 'bold' }, stepSize: 1 },
          },
          y: {
            grid: { display: false },
            ticks: {
              color: '#0f172a',
              font: { size: 12, weight: 'bold' },
              autoSkip: false,
            },
          },
        },
      },
    });
  }
}

import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { ClassAnalysisService, ClassInfo } from './class-analysis.service';
import { ClassAnalysisFull } from './class-analysis.models';
import { ClassService } from '../../core/services/class.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-class-analysis',
  imports: [Sidebar, FormsModule],
  templateUrl: './class-analysis.html',
  styleUrl: './class-analysis.css'
})
export class ClassAnalysis implements OnInit {
  private service = inject(ClassAnalysisService);
  private classService = inject(ClassService);

  sidebarOpen = signal(false);

  // ── State ──
  loading = signal(true);
  error = signal<string | null>(null);
  data = signal<ClassAnalysisFull | null>(null);

  // Filters
  selectedTerm = signal<number>(0); // 0 = current, 1 = first, 2 = second
  selectedClassId = signal<number>(0);

  // Classes list for selector
  classes = signal<ClassInfo[]>([]);
  classesLoading = signal(false);

  // Computed
  overview = computed(() => this.data()?.overview ?? null);
  subjectPerformance = computed(() => this.data()?.subjectPerformance ?? []);
  attendanceTrends = computed(() => this.data()?.attendanceTrends ?? []);
  topStudents = computed(() => this.data()?.topStudents ?? []);
  atRiskStudents = computed(() => this.data()?.atRiskStudents ?? []);
  weaknessAnalysis = computed(() => this.data()?.weaknessAnalysis ?? []);
  students = computed(() => this.data()?.students ?? []);

  showStudentTable = signal(false);
  exportMenuOpen = signal(false);

  // ── Lifecycle ──
  ngOnInit() {
    this.loadClasses();
  }

  // ── Data Loading ──
  private loadClasses() {
    this.classesLoading.set(true);
    this.classService.getMyClassesCurrentYear().subscribe({
      next: (res: any) => {
        const items = res?.data ?? res ?? [];
        const list: ClassInfo[] = Array.isArray(items)
          ? items.map((c: any) => ({ id: c.id, name: c.name, gradeLevelName: c.gradeLevelName }))
          : [];
        this.classes.set(list);
        if (list.length > 0 && this.selectedClassId() === 0) {
          this.selectedClassId.set(list[0].id);
          this.loadData();
        }
        this.classesLoading.set(false);
      },
      error: () => {
        this.classesLoading.set(false);
      }
    });
  }

  loadData() {
    const classId = this.selectedClassId();
    if (!classId) return;

    this.loading.set(true);
    this.error.set(null);

    const term = this.selectedTerm() > 0 ? this.selectedTerm() : undefined;

    this.service.getFullAnalysis(classId, term).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.data.set(res.data);
        } else {
          // If we get a 404 or empty data, set error message
          this.error.set(res.message || 'حدث خطأ أثناء تحميل البيانات');
        }
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Class analysis error:', err);
        this.error.set('فشل تحميل بيانات التحليل. يرجى المحاولة مرة أخرى.');
        this.loading.set(false);
      }
    });
  }

  // ── Actions ──
  onClassChange() {
    this.loadData();
  }

  onTermChange(term: number) {
    this.selectedTerm.set(term);
    this.loadData();
  }

  refreshData() {
    this.loadData();
  }

  toggleStudentTable() {
    this.showStudentTable.update(v => !v);
  }

  toggleExportMenu() {
    this.exportMenuOpen.update(v => !v);
  }

  closeExportMenu() {
    this.exportMenuOpen.set(false);
  }

  exportAsPdf() {
    this.exportMenuOpen.set(false);
    window.print();
  }

  exportAsCsv() {
    this.exportMenuOpen.set(false);
    // Simple CSV export for students
    const s = this.students();
    if (s.length === 0) return;
    const headers = ['الطالب', 'المعدل', 'نسبة الحضور', 'عدد أيام الغياب', 'الحالة'];
    const rows = s.map(st => [st.studentName, st.averageScore.toFixed(1), st.attendanceRate.toFixed(1) + '%', st.absenceCount.toString(), st.status]);
    const csv = [headers, ...rows].map(r => r.join(',')).join('\n');
    const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `class-analysis-${this.overview()?.className ?? 'class'}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  getAttendanceGradient(rate: number): string {
    if (rate >= 95) return 'linear-gradient(180deg, #059669, #10b981)';
    if (rate >= 85) return 'linear-gradient(180deg, #059669, #34d399)';
    if (rate >= 75) return 'linear-gradient(180deg, #d97706, #f59e0b)';
    return 'linear-gradient(180deg, #dc2626, #f97316)';
  }

  getInitials(name: string): string {
    return name.split(' ').map(w => w[0]).join('').slice(0, 2) || '??';
  }

  severityClass(severity: string): string {
    switch (severity) {
      case 'critical': return 'sev-critical';
      case 'danger': return 'sev-danger';
      case 'warning': return 'sev-warning';
      case 'safe': return 'sev-safe';
      case 'low': return 'sev-low';
      case 'medium': return 'sev-medium';
      default: return 'sev-safe';
    }
  }

  statusClass(status: string): string {
    switch (status) {
      case 'excellent': return 'status-excellent';
      case 'at-risk': return 'status-at-risk';
      default: return 'status-active';
    }
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'excellent': return 'متميز';
      case 'at-risk': return 'تحت الخطر';
      default: return 'نشط';
    }
  }

  trendIcon(change: number): string {
    if (change > 2) return 'trending_up';
    if (change < -2) return 'trending_down';
    return 'trending_flat';
  }

  trendColor(change: number): string {
    if (change > 2) return 'text-emerald-500';
    if (change < -2) return 'text-red-500';
    return 'text-gray-400';
  }

  diffColor(diff: number): string {
    if (diff > 0) return 'text-emerald-600';
    if (diff < 0) return 'text-red-600';
    return 'text-gray-500';
  }

  diffLabel(diff: number): string {
    if (diff > 0) return `+${diff.toFixed(1)}%`;
    if (diff < 0) return `${diff.toFixed(1)}%`;
    return '0%';
  }

  getSeverityIcon(severity: string): string {
    switch (severity) {
      case 'critical': return 'dangerous';
      case 'danger': return 'warning';
      case 'warning': return 'priority_high';
      case 'safe': return 'check_circle';
      case 'low': return 'arrow_downward';
      case 'medium': return 'remove';
      default: return 'info';
    }
  }

  getSeverityLabel(severity: string): string {
    switch (severity) {
      case 'critical': return 'حرجة';
      case 'danger': return 'خطيرة';
      case 'warning': return 'تحتاج متابعة';
      case 'safe': return 'آمنة';
      case 'low': return 'منخفضة';
      case 'medium': return 'متوسطة';
      default: return 'غير معروف';
    }
  }
}

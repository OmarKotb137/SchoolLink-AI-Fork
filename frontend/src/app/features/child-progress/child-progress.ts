import { Component, signal, OnInit, inject } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { ChildProgressService, ChildProgressItem } from './child-progress.service';
import { AcademicYearService } from '../../core/services/academic-year.service';

interface AssignmentView {
  id: number;
  subject: string;
  title: string;
  deadline: string;
  status: 'submitted' | 'not-submitted' | 'late';
  score?: number;
  maxScore: number;
}

interface ExamView {
  id: number;
  subject: string;
  date: string;
  status: 'upcoming' | 'done' | 'missed';
  score?: number;
  maxScore: number;
}

@Component({
  selector: 'app-child-progress',
  imports: [Sidebar],
  templateUrl: './child-progress.html',
  styleUrl: './child-progress.css'
})
export class ChildProgress implements OnInit {
  private service = inject(ChildProgressService);
  private academicYearService = inject(AcademicYearService);

  sidebarOpen = signal(false);
  activeTab = signal<'assignments' | 'exams'>('assignments');

  children = signal<ChildProgressItem[]>([]);
  selectedChildIndex = signal<number>(0);

  student = signal<{ name: string; class: string; avgScore: number; attendance: number } | null>(null);
  assignments = signal<AssignmentView[]>([]);
  exams = signal<ExamView[]>([]);

  loading = signal(false);
  selectedTerm = signal<number>(1);

  ngOnInit() {
    this.loadData();

    this.academicYearService.getCurrentTerm().subscribe({
      next: (res) => {
        if (res?.data != null && this.selectedTerm() !== res.data) {
          this.selectedTerm.set(res.data);
          this.loadData();
        }
      }
    });
  }

  loadData() {
    this.loading.set(true);
    this.service.get(this.selectedTerm()).subscribe({
      next: (items: ChildProgressItem[]) => {
        this.loading.set(false);
        if (items.length === 0) return;
        this.children.set(items);
        this.selectedChildIndex.set(0);
        this.displayChild(0);
      },
      error: () => this.loading.set(false),
    });
  }

  private displayChild(index: number) {
    const child = this.children()[index];
    if (!child) return;
    this.student.set({
      name: child.studentName,
      class: `${child.gradeLevelName} - ${child.className}`,
      avgScore: child.avgScore,
      attendance: child.attendancePercentage,
    });
    this.assignments.set(child.assignments.map(a => ({
      id: a.id,
      subject: a.subject,
      title: a.title,
      deadline: a.deadline ?? '',
      status: a.status as AssignmentView['status'],
      score: a.score,
      maxScore: a.maxScore,
    })));
    this.exams.set(child.exams.map(e => ({
      id: e.id,
      subject: e.subject,
      date: e.date ?? '',
      status: e.status as ExamView['status'],
      score: e.score,
      maxScore: e.maxScore,
    })));
  }

  onChildChange(event: Event) {
    const idx = Number((event.target as HTMLSelectElement).value);
    this.selectedChildIndex.set(idx);
    this.displayChild(idx);
  }

  onTermChange(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    this.selectedTerm.set(Number(value));
    this.loadData();
  }

  getStatusText(s: string): string {
    const m: Record<string, string> = { submitted: 'تم التسليم', 'not-submitted': 'لم يسلّم', late: 'متأخر', upcoming: 'قادم', done: 'أدّاه', missed: 'لم يؤدّه' };
    return m[s] || s;
  }

  getStatusClass(s: string): string {
    const m: Record<string, string> = { submitted: 'bg-green-50 text-green-700', 'not-submitted': 'bg-secondary/10 text-secondary', late: 'bg-error/10 text-error', upcoming: 'bg-secondary/10 text-secondary', done: 'bg-green-50 text-green-700', missed: 'bg-error/10 text-error' };
    return m[s] || '';
  }
}

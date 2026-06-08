import { Component, signal, OnInit, inject } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ChildProgressService, ChildProgressItem } from './child-progress.service';

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
  imports: [Sidebar, Topbar],
  templateUrl: './child-progress.html',
  styleUrl: './child-progress.css'
})
export class ChildProgress implements OnInit {
  private service = inject(ChildProgressService);

  sidebarOpen = signal(false);
  activeTab = signal<'assignments' | 'exams'>('assignments');

  student = signal<{ name: string; class: string; avgScore: number; attendance: number } | null>(null);

  assignments = signal<AssignmentView[]>([]);
  exams = signal<ExamView[]>([]);

  loading = signal(false);

  ngOnInit() {
    this.loading.set(true);
    this.service.get().subscribe({
      next: (items: ChildProgressItem[]) => {
        this.loading.set(false);
        if (items.length === 0) return;
        const first = items[0];
        this.student.set({
          name: first.studentName,
          class: `${first.gradeLevelName} - ${first.className}`,
          avgScore: first.avgScore,
          attendance: first.attendancePercentage,
        });
        this.assignments.set(first.assignments.map(a => ({
          id: a.id,
          subject: a.subject,
          title: a.title,
          deadline: a.deadline ?? '',
          status: a.status as AssignmentView['status'],
          score: a.score,
          maxScore: a.maxScore,
        })));
        this.exams.set(first.exams.map(e => ({
          id: e.id,
          subject: e.subject,
          date: e.date ?? '',
          status: e.status as ExamView['status'],
          score: e.score,
          maxScore: e.maxScore,
        })));
      },
      error: () => this.loading.set(false),
    });
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

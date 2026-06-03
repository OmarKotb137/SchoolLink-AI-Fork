import { Component, signal, computed } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

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
export class ChildProgress {
  sidebarOpen = signal(false);
  activeTab = signal<'assignments' | 'exams'>('assignments');

  student = {
    name: 'أحمد محمود',
    class: 'الصف الثالث الإعدادي - أ',
    avgScore: 82,
    attendance: 93,
  };

  assignments = signal<AssignmentView[]>([
    { id: 1, subject: 'الرياضيات', title: 'تمارين المعادلات التربيعية', deadline: '2026-06-10', status: 'submitted', score: 18, maxScore: 20 },
    { id: 2, subject: 'اللغة العربية', title: 'تحليل النص الشعري', deadline: '2026-06-08', status: 'not-submitted', maxScore: 20 },
    { id: 3, subject: 'العلوم', title: 'تجربة الكهرباء', deadline: '2026-06-05', status: 'submitted', score: 15, maxScore: 20 },
    { id: 4, subject: 'اللغة الإنجليزية', title: 'قواعد الوحدة ٣', deadline: '2026-06-01', status: 'late', score: 10, maxScore: 20 },
    { id: 5, subject: 'الدراسات الاجتماعية', title: 'الخرائط الجغرافية', deadline: '2026-06-12', status: 'not-submitted', maxScore: 20 },
  ]);

  exams = signal<ExamView[]>([
    { id: 1, subject: 'الرياضيات', date: '2026-06-15', status: 'upcoming', maxScore: 100 },
    { id: 2, subject: 'اللغة العربية', date: '2026-06-10', status: 'upcoming', maxScore: 100 },
    { id: 3, subject: 'العلوم', date: '2026-06-03', status: 'done', score: 85, maxScore: 100 },
    { id: 4, subject: 'اللغة الإنجليزية', date: '2026-06-01', status: 'done', score: 72, maxScore: 100 },
    { id: 5, subject: 'الرياضيات', date: '2026-05-28', status: 'missed', maxScore: 100 },
  ]);

  getStatusText(s: string): string {
    const m: Record<string, string> = { submitted: 'تم التسليم', 'not-submitted': 'لم يسلّم', late: 'متأخر', upcoming: 'قادم', done: 'أدّاه', missed: 'لم يؤدّه' };
    return m[s] || s;
  }

  getStatusClass(s: string): string {
    const m: Record<string, string> = { submitted: 'bg-green-50 text-green-700', 'not-submitted': 'bg-secondary/10 text-secondary', late: 'bg-error/10 text-error', upcoming: 'bg-secondary/10 text-secondary', done: 'bg-green-50 text-green-700', missed: 'bg-error/10 text-error' };
    return m[s] || '';
  }
}

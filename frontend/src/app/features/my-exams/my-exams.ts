import { Component, signal, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

interface CurrentExam {
  id: number;
  subject: string;
  name: string;
  date: string;
  time: string;
  duration: number;
  questionCount: number;
  status: 'upcoming' | 'active' | 'ended';
  score?: number;
  totalScore?: number;
}

interface PastExam {
  id: number;
  name: string;
  subject: string;
  year: string;
  questionCount: number;
  tried: boolean;
  score?: number;
}

@Component({
  selector: 'app-my-exams',
  imports: [Sidebar, Topbar],
  templateUrl: './my-exams.html',
  styleUrl: './my-exams.css'
})
export class MyExams {
  private router = inject(Router);
  sidebarOpen = signal(false);
  activeTab = signal<string>('current');
  currentFilter = signal<string>('all');
  pastSubjectFilter = signal<string>('all');
  pastYearFilter = signal<string>('all');

  currentExams = signal<CurrentExam[]>([
    { id: 1, subject: 'الرياضيات', name: 'اختبار منتصف الفصل - المعادلات', date: '2026-06-10', time: '10:00 ص', duration: 90, questionCount: 15, status: 'upcoming' },
    { id: 2, subject: 'العلوم', name: 'امتحان العلوم الشهري', date: '2026-06-05', time: '11:30 ص', duration: 60, questionCount: 20, status: 'upcoming' },
    { id: 3, subject: 'اللغة العربية', name: 'اختبار قصير - النحو', date: '2026-06-03', time: '09:00 ص', duration: 45, questionCount: 10, status: 'active' },
    { id: 4, subject: 'اللغة الإنجليزية', name: 'امتحان الاستماع والمحادثة', date: '2026-06-01', time: '10:00 ص', duration: 60, questionCount: 25, status: 'ended', score: 42, totalScore: 50 },
    { id: 5, subject: 'الرياضيات', name: 'اختبار الأسبوع الماضي', date: '2026-05-28', time: '09:00 ص', duration: 45, questionCount: 10, status: 'ended', score: 18, totalScore: 20 },
  ]);

  pastExams = signal<PastExam[]>([
    { id: 1, name: 'امتحان الرياضيات - العام الماضي', subject: 'الرياضيات', year: '2025', questionCount: 20, tried: true, score: 85 },
    { id: 2, name: 'اختبار العلوم - الفصل الأول', subject: 'العلوم', year: '2025', questionCount: 15, tried: false },
    { id: 3, name: 'امتحان اللغة العربية', subject: 'اللغة العربية', year: '2025', questionCount: 25, tried: true, score: 72 },
    { id: 4, name: 'اختبار الإنجليزي - نصف العام', subject: 'اللغة الإنجليزية', year: '2024', questionCount: 20, tried: false },
    { id: 5, name: 'مسابقة الرياضيات', subject: 'الرياضيات', year: '2024', questionCount: 30, tried: true, score: 90 },
    { id: 6, name: 'امتحان الدراسات - الترم الثاني', subject: 'الدراسات الاجتماعية', year: '2025', questionCount: 18, tried: false },
  ]);

  filteredCurrentExams = computed(() => {
    const f = this.currentFilter();
    const all = this.currentExams();
    if (f === 'all') return all;
    return all.filter(e => e.status === f);
  });

  filteredPastExams = computed(() => {
    let items = this.pastExams();
    const subj = this.pastSubjectFilter();
    const year = this.pastYearFilter();
    if (subj !== 'all') items = items.filter(e => e.subject === subj);
    if (year !== 'all') items = items.filter(e => e.year === year);
    return items;
  });

  pastStats = computed(() => {
    const all = this.pastExams();
    const tried = all.filter(e => e.tried);
    return {
      total: all.length,
      tried: tried.length,
      avgScore: tried.length ? Math.round(tried.reduce((s, e) => s + (e.score || 0), 0) / tried.length) : 0,
    };
  });

  pastSubjects = computed(() => [...new Set(this.pastExams().map(e => e.subject))]);
  pastYears = computed(() => [...new Set(this.pastExams().map(e => e.year))]);

  getStatusText(s: string): string {
    const m: Record<string, string> = { upcoming: 'قادم', active: 'متاح الآن', ended: 'منتهٍ' };
    return m[s] || s;
  }

  getStatusClass(s: string): string {
    const m: Record<string, string> = { upcoming: 'bg-secondary/10 text-secondary', active: 'bg-green-50 text-green-700', ended: 'bg-surface-container-high text-outline' };
    return m[s] || '';
  }

  startExam(id: number) {
    this.router.navigate(['/exam-generator']);
  }

  startPastExam(id: number) {
    this.router.navigate(['/exam-generator']);
  }
}

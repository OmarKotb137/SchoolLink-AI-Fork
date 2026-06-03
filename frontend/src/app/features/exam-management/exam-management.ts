import { Component, signal, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

interface ExamQuestion {
  id: number;
  type: 'mcq' | 'true-false' | 'essay';
  text: string;
  options?: string[];
  correctAnswer: string;
}

interface Exam {
  id: number;
  name: string;
  subject: string;
  class: string;
  date: string;
  startTime: string;
  endTime: string;
  duration: number;
  questionCount: number;
  status: 'upcoming' | 'active' | 'ended' | 'draft';
  avgScore?: number;
  submitted?: number;
  total?: number;
}

@Component({
  selector: 'app-exam-management',
  imports: [Sidebar, Topbar],
  templateUrl: './exam-management.html',
  styleUrl: './exam-management.css'
})
export class ExamManagement {
  private router = inject(Router);
  sidebarOpen = signal(false);
  activeTab = signal<string>('all');
  showAddModal = signal(false);
  showViewModal = signal(false);
  viewingExam = signal<Exam | null>(null);
  editingExam = signal<Exam | null>(null);

  newName = signal('');
  newSubject = signal('');
  newClass = signal('');
  newDate = signal('');
  newStart = signal('');
  newEnd = signal('');

  subjects = ['الرياضيات', 'اللغة العربية', 'اللغة الإنجليزية', 'العلوم', 'الدراسات الاجتماعية'];
  classes = ['الصف الثالث - أ', 'الصف الثالث - ب', 'الصف الثاني - أ', 'الصف الثاني - ب'];

  exams = signal<Exam[]>([
    { id: 1, name: 'اختبار منتصف الفصل', subject: 'الرياضيات', class: 'الصف الثالث - أ', date: '2026-06-15', startTime: '10:00', endTime: '11:30', duration: 90, questionCount: 15, status: 'upcoming', total: 35 },
    { id: 2, name: 'امتحان اللغة العربية الشهري', subject: 'اللغة العربية', class: 'الصف الثالث - ب', date: '2026-06-10', startTime: '09:00', endTime: '10:30', duration: 90, questionCount: 20, status: 'upcoming', total: 32 },
    { id: 3, name: 'اختبار قصير - العلوم', subject: 'العلوم', class: 'الصف الثالث - أ', date: '2026-06-03', startTime: '11:00', endTime: '11:45', duration: 45, questionCount: 10, status: 'active', total: 35 },
    { id: 4, name: 'امتحان الإنجليزي الشهري', subject: 'اللغة الإنجليزية', class: 'الصف الثالث - أ', date: '2026-06-01', startTime: '10:00', endTime: '11:30', duration: 90, questionCount: 25, status: 'ended', avgScore: 78, submitted: 33, total: 35 },
    { id: 5, name: 'مراجعة الجبر', subject: 'الرياضيات', class: 'الصف الثالث - ب', date: '2026-06-20', startTime: '09:00', endTime: '10:00', duration: 60, questionCount: 12, status: 'draft', total: 30 },
  ]);

  filteredExams = computed(() => {
    const tab = this.activeTab();
    if (tab === 'all') return this.exams();
    return this.exams().filter(e => e.status === tab);
  });

  stats = computed(() => {
    const all = this.exams();
    return {
      total: all.length,
      upcoming: all.filter(e => e.status === 'upcoming').length,
      ended: all.filter(e => e.status === 'ended').length,
      avgScore: all.filter(e => e.avgScore).length ? Math.round(all.filter(e => e.avgScore).reduce((s, e) => s + (e.avgScore || 0), 0) / all.filter(e => e.avgScore).length) : 0,
    };
  });

  setActiveTab(tab: string) { this.activeTab.set(tab); }

  goToGenerator() {
    this.router.navigate(['/exam-generator']);
  }

  openAddModal() {
    this.editingExam.set(null);
    this.newName.set('');
    this.newSubject.set('');
    this.newClass.set('');
    this.newDate.set('');
    this.newStart.set('');
    this.newEnd.set('');
    this.showAddModal.set(true);
  }

  openEditModal(e: Exam) {
    this.editingExam.set(e);
    this.newName.set(e.name);
    this.newSubject.set(e.subject);
    this.newClass.set(e.class);
    this.newDate.set(e.date);
    this.newStart.set(e.startTime);
    this.newEnd.set(e.endTime);
    this.showAddModal.set(true);
  }

  openViewModal(e: Exam) {
    this.viewingExam.set(e);
    this.showViewModal.set(true);
  }

  closeModals() {
    this.showAddModal.set(false);
    this.showViewModal.set(false);
    this.viewingExam.set(null);
  }

  saveExam() {
    const e = this.editingExam();
    if (e) {
      this.exams.update(list => list.map(x => x.id === e.id ? { ...x, name: this.newName(), subject: this.newSubject(), class: this.newClass(), date: this.newDate(), startTime: this.newStart(), endTime: this.newEnd() } : x));
    } else {
      this.exams.update(list => [...list, { id: Date.now(), name: this.newName(), subject: this.newSubject(), class: this.newClass(), date: this.newDate(), startTime: this.newStart(), endTime: this.newEnd(), duration: 60, questionCount: 0, status: 'draft', total: 35 }]);
    }
    this.closeModals();
  }

  deleteExam(id: number) {
    this.exams.update(list => list.filter(x => x.id !== id));
  }

  publishExam(id: number) {
    this.exams.update(list => list.map(x => x.id === id ? { ...x, status: 'upcoming' as const } : x));
  }

  getStatusText(status: string): string {
    const map: Record<string, string> = { upcoming: 'قادم', active: 'جاري الآن', ended: 'منتهٍ', draft: 'مسودة' };
    return map[status] || status;
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = { upcoming: 'bg-secondary/10 text-secondary', active: 'bg-green-50 text-green-700', ended: 'bg-surface-container-high text-outline', draft: 'bg-tertiary-fixed/20 text-tertiary' };
    return map[status] || '';
  }

  getQuestionsForExam(e: Exam): ExamQuestion[] {
    return [
      { id: 1, type: 'mcq', text: 'ما هو حل المعادلة x² - 5x + 6 = 0؟', options: ['x=2, x=3', 'x=1, x=6', 'x=-2, x=-3', 'لا يوجد حل'], correctAnswer: 'x=2, x=3' },
      { id: 2, type: 'true-false', text: 'العدد 17 هو عدد أولي.', correctAnswer: 'صواب' },
      { id: 3, type: 'essay', text: 'اشرح بالتفصيل كيفية حساب مساحة الدائرة.', correctAnswer: '' },
    ];
  }
}

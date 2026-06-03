import { Component, signal, computed } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

interface Assignment {
  id: number;
  title: string;
  subject: string;
  class: string;
  deadline: string;
  submitted: number;
  total: number;
  status: 'active' | 'closed' | 'draft';
}

interface Question {
  id: number;
  type: 'mcq' | 'true-false' | 'essay';
  text: string;
  options?: string[];
  correctAnswer: string;
}

@Component({
  selector: 'app-assignment-management',
  imports: [Sidebar, Topbar],
  templateUrl: './assignment-management.html',
  styleUrl: './assignment-management.css'
})
export class AssignmentManagement {
  sidebarOpen = signal(false);
  showAddModal = signal(false);
  showViewModal = signal(false);
  viewingAssignment = signal<Assignment | null>(null);
  editingAssignment = signal<Assignment | null>(null);

  newTitle = signal('');
  newSubject = signal('');
  newClass = signal('');
  newDeadline = signal('');
  questions = signal<Question[]>([]);

  assignments = signal<Assignment[]>([
    { id: 1, title: 'تمارين المعادلات', subject: 'الرياضيات', class: 'الصف الثالث - أ', deadline: '2026-06-10', submitted: 28, total: 35, status: 'active' },
    { id: 2, title: 'تحليل النصوص', subject: 'اللغة العربية', class: 'الصف الثالث - ب', deadline: '2026-06-08', submitted: 30, total: 32, status: 'active' },
    { id: 3, title: 'قواعد اللغة', subject: 'اللغة الإنجليزية', class: 'الصف الثالث - أ', deadline: '2026-06-05', submitted: 35, total: 35, status: 'closed' },
    { id: 4, title: 'تجارب الكهرباء', subject: 'العلوم', class: 'الصف الثالث - أ', deadline: '2026-06-15', submitted: 0, total: 35, status: 'draft' },
    { id: 5, title: 'مسائل الجبر', subject: 'الرياضيات', class: 'الصف الثالث - ب', deadline: '2026-06-12', submitted: 15, total: 30, status: 'active' },
  ]);

  stats = computed(() => {
    const all = this.assignments();
    return {
      total: all.length,
      active: all.filter(a => a.status === 'active').length,
      avgDelivery: all.length ? Math.round(all.reduce((s, a) => s + (a.submitted / a.total) * 100, 0) / all.length) : 0,
      overdue: all.filter(a => a.status === 'active' && a.submitted < a.total).length,
    };
  });

  subjects = ['الرياضيات', 'اللغة العربية', 'اللغة الإنجليزية', 'العلوم', 'الدراسات الاجتماعية'];
  classes = ['الصف الثالث - أ', 'الصف الثالث - ب', 'الصف الثاني - أ', 'الصف الثاني - ب'];

  openAddModal() {
    this.editingAssignment.set(null);
    this.newTitle.set('');
    this.newSubject.set('');
    this.newClass.set('');
    this.newDeadline.set('');
    this.questions.set([]);
    this.showAddModal.set(true);
  }

  openEditModal(a: Assignment) {
    this.editingAssignment.set(a);
    this.newTitle.set(a.title);
    this.newSubject.set(a.subject);
    this.newClass.set(a.class);
    this.newDeadline.set(a.deadline);
    this.showAddModal.set(true);
  }

  openViewModal(a: Assignment) {
    this.viewingAssignment.set(a);
    this.showViewModal.set(true);
  }

  closeModals() {
    this.showAddModal.set(false);
    this.showViewModal.set(false);
    this.viewingAssignment.set(null);
  }

  addQuestion() {
    this.questions.update(q => [...q, { id: Date.now(), type: 'mcq', text: '', options: ['', '', '', ''], correctAnswer: '0' }]);
  }

  removeQuestion(id: number) {
    this.questions.update(q => q.filter(x => x.id !== id));
  }

  updateQuestionType(id: number, type: 'mcq' | 'true-false' | 'essay') {
    this.questions.update(q => q.map(x => x.id === id ? { ...x, type, options: type === 'mcq' ? ['', '', '', ''] : type === 'true-false' ? ['صواب', 'خطأ'] : undefined, correctAnswer: '' } : x));
  }

  updateQuestionText(id: number, text: string) {
    this.questions.update(q => q.map(x => x.id === id ? { ...x, text } : x));
  }

  updateQuestionOption(qId: number, oIdx: number, value: string) {
    this.questions.update(q => q.map(x => x.id === qId ? { ...x, options: x.options?.map((o, i) => i === oIdx ? value : o) } : x));
  }

  updateQuestionAnswer(id: number, answer: string) {
    this.questions.update(q => q.map(x => x.id === id ? { ...x, correctAnswer: answer } : x));
  }

  saveAssignment() {
    const a = this.editingAssignment();
    if (a) {
      this.assignments.update(list => list.map(x => x.id === a.id ? { ...x, title: this.newTitle(), subject: this.newSubject(), class: this.newClass(), deadline: this.newDeadline() } : x));
    } else {
      this.assignments.update(list => [...list, { id: Date.now(), title: this.newTitle(), subject: this.newSubject(), class: this.newClass(), deadline: this.newDeadline(), submitted: 0, total: 35, status: 'draft' }]);
    }
    this.closeModals();
  }

  deleteAssignment(id: number) {
    this.assignments.update(list => list.filter(x => x.id !== id));
  }

  getStatusText(status: string): string {
    const map: Record<string, string> = { active: 'نشط', closed: 'منتهٍ', draft: 'مسودة' };
    return map[status] || status;
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = { active: 'bg-primary/10 text-primary', closed: 'bg-surface-container-high text-outline', draft: 'bg-tertiary-fixed/20 text-tertiary' };
    return map[status] || '';
  }

  str(i: number): string { return String(i); }

  getTypeLabel(type: string): string {
    const map: Record<string, string> = { mcq: 'اختيار من متعدد', 'true-false': 'صح وخطأ', essay: 'مقالي' };
    return map[type] || type;
  }

  getQuestionsForAssignment(a: Assignment): Question[] {
    return [
      { id: 1, type: 'mcq', text: 'ما هو حل المعادلة x² - 5x + 6 = 0؟', options: ['x=2, x=3', 'x=1, x=6', 'x=-2, x=-3', 'لا يوجد حل'], correctAnswer: 'x=2, x=3' },
      { id: 2, type: 'true-false', text: 'المعادلة التربيعية لها جذران حقيقيان دائماً.', correctAnswer: 'خطأ' },
      { id: 3, type: 'essay', text: 'اشرح طريقة حل المعادلة التربيعية باستخدام القانون العام.', correctAnswer: '' },
    ];
  }
}

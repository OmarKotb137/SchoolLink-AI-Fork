import { Component, signal, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { AssignmentManagerService, AssignmentItem, Question, AssignmentDetail, Stats } from './assignment-manager.service';

@Component({
  selector: 'app-assignment-management',
  imports: [Sidebar, Topbar, FormsModule, RouterModule],
  templateUrl: './assignment-management.html',
  styleUrl: './assignment-management.css'
})
export class AssignmentManagement implements OnInit {
  private api = inject(AssignmentManagerService);

  sidebarOpen = signal(false);
  showAddModal = signal(false);
  showViewModal = signal(false);
  viewingAssignment = signal<AssignmentDetail | null>(null);
  editingAssignment = signal<AssignmentItem | null>(null);

  newTitle = signal('');
  newSubjectId = signal<number | null>(null);
  newClassId = signal<number | null>(null);
  newDeadline = signal('');
  questions = signal<Question[]>([]);

  assignments = signal<AssignmentItem[]>([]);
  stats = signal<Stats>({ total: 0, active: 0, avgDelivery: 0, overdue: 0 });

  subjects = signal<{ id: number; name: string }[]>([]);
  classes = signal<{ id: number; name: string }[]>([]);

  ngOnInit() {
    this.loadAll();
  }

  private loadAll() {
    this.api.getAll().subscribe(r => {
      if (r.isSuccess) {
        this.assignments.set(r.data);
      }
    });
    this.api.getStats().subscribe(r => {
      if (r.isSuccess) {
        this.stats.set(r.data);
      }
    });
    this.api.getSubjects().subscribe(r => this.subjects.set(r));
    this.api.getClasses().subscribe(r => this.classes.set(r));
  }

  openAddModal() {
    this.editingAssignment.set(null);
    this.newTitle.set('');
    this.newSubjectId.set(null);
    this.newClassId.set(null);
    this.newDeadline.set('');
    this.questions.set([]);
    this.showAddModal.set(true);
  }

  openEditModal(a: AssignmentItem) {
    this.api.getById(a.id).subscribe(detail => {
      this.editingAssignment.set(a);
      this.newTitle.set(a.title);
      const subj = this.subjects().find(s => s.name === a.subject);
      this.newSubjectId.set(subj ? subj.id : null);
      const cls = this.classes().find(c => c.name === a.class);
      this.newClassId.set(cls ? cls.id : null);
      this.newDeadline.set(a.deadline);
      this.questions.set(detail.questions.map(q => ({
        ...q,
        correctAnswer: q.type === 'mcq' ? String(q.options?.indexOf(q.correctAnswer) !== -1 ? q.options?.indexOf(q.correctAnswer) : 0) : q.correctAnswer
      })));
      this.showAddModal.set(true);
    });
  }

  openViewModal(a: AssignmentItem) {
    this.api.getById(a.id).subscribe(detail => {
      this.viewingAssignment.set(detail);
      this.showViewModal.set(true);
    });
  }

  closeModals() {
    this.showAddModal.set(false);
    this.showViewModal.set(false);
    this.viewingAssignment.set(null);
  }

  addQuestion() {
    this.questions.update(q => [...q, { id: Date.now(), type: 'mcq', text: '', options: ['', '', '', ''], correctAnswer: '0', points: 5 }]);
  }

  removeQuestion(id: number) {
    this.questions.update(q => q.filter(x => x.id !== id));
  }

  updateQuestionType(id: number, type: string) {
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

  updateQuestionPoints(id: number, points: number) {
    this.questions.update(q => q.map(x => x.id === id ? { ...x, points } : x));
  }

  saveAssignment() {
    const payload = {
      title: this.newTitle(),
      subjectId: this.newSubjectId() ?? 0,
      classId: this.newClassId() ?? 0,
      deadline: this.newDeadline(),
      questions: this.questions().map(q => ({
        type: q.type,
        text: q.text,
        options: q.options ?? [],
        correctAnswer: q.type === 'mcq' ? (q.options ? q.options[Number(q.correctAnswer)] ?? '' : '') : q.correctAnswer,
        points: Number(q.points) || (q.type === 'essay' ? 10 : 5)
      }))
    };

    const existing = this.editingAssignment();
    if (existing) {
      this.api.update(existing.id, payload).subscribe(r => {
        if (r.isSuccess) this.loadAll();
      });
    } else {
      this.api.create(payload).subscribe(r => {
        if (r.isSuccess) this.loadAll();
      });
    }
    this.closeModals();
  }

  deleteAssignment(id: number) {
    this.api.delete(id).subscribe(r => {
      if (r.isSuccess) this.loadAll();
    });
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

}

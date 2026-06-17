import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ExamManagerService, ExamItem, ExamDetail, ExamStats } from './exam-manager.service';

@Component({
  selector: 'app-exam-management',
  imports: [Sidebar, Topbar, FormsModule],
  templateUrl: './exam-management.html',
  styleUrl: './exam-management.css'
})
export class ExamManagement implements OnInit {
  private router = inject(Router);
  private api = inject(ExamManagerService);

  sidebarOpen = signal(false);
  activeTab = signal<string>('all');
  showAddModal = signal(false);
  showViewModal = signal(false);
  showPublishConfirm = signal(false);
  viewingExam = signal<ExamDetail | null>(null);
  editingExam = signal<ExamItem | null>(null);
  publishingExam = signal<ExamItem | null>(null);
  formError = signal('');

  newName = signal('');
  newSubjectId = signal<number | null>(null);
  newClassId = signal<number | null>(null);
  newDate = signal('');
  newStart = signal('');
  newEnd = signal('');

  exams = signal<ExamItem[]>([]);
  stats = signal<ExamStats>({ total: 0, upcoming: 0, ended: 0, avgScore: 0 });
  subjects = signal<{ id: number; name: string }[]>([]);
  classes = signal<{ id: number; name: string }[]>([]);

  filteredExams = computed(() => {
    const tab = this.activeTab();
    if (tab === 'all') return this.exams();
    return this.exams().filter(e => e.status === tab);
  });

  calculatedDuration = computed(() => this.calculateDurationMinutes(this.newStart(), this.newEnd()));

  ngOnInit() {
    this.loadAll();
  }

  private loadAll() {
    this.api.getAll().subscribe(r => {
      if (r.isSuccess) this.exams.set(r.data);
    });
    this.api.getStats().subscribe(r => {
      if (r.isSuccess) this.stats.set(r.data);
    });
    this.api.getSubjects().subscribe(r => this.subjects.set(r));
    this.api.getClasses().subscribe(r => this.classes.set(r));
  }

  setActiveTab(tab: string) { this.activeTab.set(tab); }

  goToGenerator() {
    this.router.navigate(['/exam-generator']);
  }

  openAddModal() {
    this.editingExam.set(null);
    this.newName.set('');
    this.newSubjectId.set(null);
    this.newClassId.set(null);
    this.newDate.set('');
    this.newStart.set('');
    this.newEnd.set('');
    this.formError.set('');
    this.showAddModal.set(true);
  }

  openEditModal(e: ExamItem) {
    this.editingExam.set(e);
    this.newName.set(e.name);
    const subj = this.subjects().find(s => s.name === e.subject);
    this.newSubjectId.set(subj ? subj.id : null);
    const cls = this.classes().find(c => c.name === e.class);
    this.newClassId.set(cls ? cls.id : null);
    this.newDate.set(e.date);
    this.newStart.set(e.startTime);
    this.newEnd.set(e.endTime);
    this.formError.set('');
    this.showAddModal.set(true);
  }

  openViewModal(e: ExamItem) {
    this.api.getById(e.id).subscribe(detail => {
      this.viewingExam.set(detail);
      this.showViewModal.set(true);
    });
  }

  closeModals() {
    this.showAddModal.set(false);
    this.showViewModal.set(false);
    this.showPublishConfirm.set(false);
    this.viewingExam.set(null);
    this.publishingExam.set(null);
    this.formError.set('');
  }

  saveExam() {
    const durationMinutes = this.calculatedDuration();
    if (!this.newName().trim() || !this.newSubjectId() || !this.newClassId() || !this.newDate() || !this.newStart() || !this.newEnd()) {
      this.formError.set('من فضلك اكمل بيانات الامتحان قبل الحفظ');
      return;
    }

    if (durationMinutes <= 0) {
      this.formError.set('وقت النهاية يجب ان يكون بعد وقت البداية');
      return;
    }

    const payload = {
      title: this.newName().trim(),
      subjectId: this.newSubjectId() ?? 0,
      classId: this.newClassId() ?? 0,
      date: this.newDate(),
      startTime: this.newStart(),
      endTime: this.newEnd(),
      durationMinutes
    };

    const existing = this.editingExam();
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

  deleteExam(id: number) {
    this.api.delete(id).subscribe(r => {
      if (r.isSuccess) this.loadAll();
    });
  }

  publishExam(id: number) {
    const exam = this.exams().find(e => e.id === id);
    if (!exam) return;
    this.publishingExam.set(exam);
    this.showPublishConfirm.set(true);
  }

  confirmPublish() {
    const exam = this.publishingExam();
    if (!exam) return;

    this.api.publish(exam.id).subscribe(r => {
      if (r.isSuccess) this.loadAll();
      this.closeModals();
    });
  }

  togglePublishResults(examId: number, publish: boolean) {
    if (publish) {
      this.api.publishResults(examId).subscribe(r => {
        if (r.isSuccess) this.loadAll();
      });
    } else {
      this.api.unpublishResults(examId).subscribe(r => {
        if (r.isSuccess) this.loadAll();
      });
    }
  }

  private calculateDurationMinutes(start: string, end: string): number {
    if (!start || !end) return 0;

    const [startHours, startMinutes] = start.split(':').map(Number);
    const [endHours, endMinutes] = end.split(':').map(Number);
    if ([startHours, startMinutes, endHours, endMinutes].some(value => Number.isNaN(value))) return 0;

    return (endHours * 60 + endMinutes) - (startHours * 60 + startMinutes);
  }

  getStatusText(status: string): string {
    const map: Record<string, string> = { upcoming: 'قادم', active: 'جاري الآن', ended: 'منتهٍ', draft: 'مسودة' };
    return map[status] || status;
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = { upcoming: 'bg-secondary/10 text-secondary', active: 'bg-green-50 text-green-700', ended: 'bg-surface-container-high text-outline', draft: 'bg-tertiary-fixed/20 text-tertiary' };
    return map[status] || '';
  }
}

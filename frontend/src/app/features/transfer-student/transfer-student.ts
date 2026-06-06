import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { EnrollmentService, Enrollment, TransferHistory } from '../../core/services/enrollment.service';
import { ClassService, ClassEntity } from '../../core/services/class.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-transfer-student',
  imports: [Sidebar, Topbar],
  templateUrl: './transfer-student.html',
  styleUrl: './transfer-student.css'
})
export class TransferStudent implements OnInit {
  sidebarOpen = signal(false);
  
  private enrollmentService = inject(EnrollmentService);
  private classService = inject(ClassService);
  private academicYearService = inject(AcademicYearService);

  searchQuery = signal('');
  selectedSourceClassId = signal<number | null>(null);
  selectedStudent = signal<Enrollment | null>(null);
  targetClassId = signal<number | null>(null);
  transferReason = signal('');
  
  showError = signal(false);
  showSuccess = signal(false);
  isLoadingStudents = signal(false);
  isTransferring = signal(false);

  lastTransfer = signal<{ fromClass: string; toClass: string } | null>(null);

  allClasses = signal<ClassEntity[]>([]);
  students = signal<Enrollment[]>([]);
  transferHistory = signal<TransferHistory[]>([]);
  currentAcademicYearId = signal<number | null>(null);

  filteredStudents = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    if (!q) return this.students();
    return this.students().filter(s => s.studentName?.toLowerCase().includes(q));
  });

  availableTargetClasses = computed(() => {
    const sourceClassId = this.selectedSourceClassId();
    return this.allClasses().filter(c => c.id !== sourceClassId);
  });

  ngOnInit() {
    this.academicYearService.getAll().subscribe(years => {
      const currentYear = years.find(y => y.isCurrent);
      if (currentYear) {
        this.currentAcademicYearId.set(currentYear.id);
        this.loadClasses();
        this.loadTransferHistory(currentYear.id);
      }
    });
  }

  loadClasses() {
    this.classService.getAll().subscribe(classes => {
      this.allClasses.set(classes);
    });
  }

  loadTransferHistory(academicYearId: number) {
    this.enrollmentService.getTransferHistory(academicYearId).subscribe(history => {
      this.transferHistory.set(history);
    });
  }

  onSourceClassChange(classIdStr: string) {
    const classId = parseInt(classIdStr, 10);
    if (isNaN(classId)) {
      this.selectedSourceClassId.set(null);
      this.students.set([]);
      this.cancelTransfer();
      return;
    }

    this.selectedSourceClassId.set(classId);
    this.cancelTransfer();
    
    const yearId = this.currentAcademicYearId();
    if (!yearId) return;

    this.isLoadingStudents.set(true);
    this.enrollmentService.getByClass(classId, yearId, true)
      .pipe(finalize(() => this.isLoadingStudents.set(false)))
      .subscribe(enrollments => {
        this.students.set(enrollments);
      });
  }

  selectStudent(s: Enrollment) {
    this.selectedStudent.set(s);
    this.targetClassId.set(null);
    this.transferReason.set('');
    this.showError.set(false);
  }

  cancelTransfer() {
    this.selectedStudent.set(null);
    this.targetClassId.set(null);
    this.transferReason.set('');
    this.showError.set(false);
  }

  confirmTransfer() {
    const student = this.selectedStudent();
    const targetId = this.targetClassId();
    const yearId = this.currentAcademicYearId();

    if (!student || !targetId || !yearId) {
      this.showError.set(true);
      return;
    }

    this.showError.set(false);
    this.isTransferring.set(true);

    const fromClassName = this.allClasses().find(c => c.id === this.selectedSourceClassId())?.name || '';
    const targetClassName = this.allClasses().find(c => c.id === targetId)?.name || '';

    this.enrollmentService.transferStudent({
      currentEnrollmentId: student.id,
      newClassId: targetId,
      transferDate: new Date().toISOString().split('T')[0],
      transferReason: this.transferReason() || undefined
    })
    .pipe(finalize(() => this.isTransferring.set(false)))
    .subscribe({
      next: () => {
        // Remove student from the current list
        this.students.update(list => list.filter(s => s.id !== student.id));
        
        // Update history
        this.loadTransferHistory(yearId);

        this.lastTransfer.set({ fromClass: fromClassName, toClass: targetClassName });
        this.showSuccess.set(true);
        this.selectedStudent.set(null);
        this.targetClassId.set(null);
        this.transferReason.set('');

        setTimeout(() => {
          this.showSuccess.set(false);
          this.lastTransfer.set(null);
        }, 3000);
      },
      error: () => {
        // Handle error (e.g., show notification)
      }
    });
  }
}

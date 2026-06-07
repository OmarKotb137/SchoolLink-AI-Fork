import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AcademicYear, AcademicYearService } from '../../core/services/academic-year.service';
import {
  ClassStudentBrowserItem,
  ClassStudentsBrowserResult,
  ClassStudentsBrowserService
} from '../../core/services/class-students-browser.service';
import { ClassEntity, ClassService } from '../../core/services/class.service';
import { GradeLevel, GradeLevelService } from '../../core/services/grade-level.service';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-class-management',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './class-management.html',
  styleUrl: './class-management.css',
})
export class ClassManagement implements OnInit {
  sidebarOpen = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'المشرف';

  private classService = inject(ClassService);
  private classStudentsBrowserService = inject(ClassStudentsBrowserService);
  private gradeLevelService = inject(GradeLevelService);
  private academicYearService = inject(AcademicYearService);

  classes = signal<ClassEntity[]>([]);
  gradeLevels = signal<GradeLevel[]>([]);
  academicYears = signal<AcademicYear[]>([]);

  currentPage = signal(1);
  itemsPerPage = signal(10);

  paginatedClasses = computed(() => {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    return this.classes().slice(start, start + this.itemsPerPage());
  });

  totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.classes().length / this.itemsPerPage()));
  });

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
    }
  }

  goToPage(page: number) {
    this.currentPage.set(page);
  }

  selectedGradeFilter: number | null = null;
  selectedYearFilter: number | null = null;

  editingClassId = signal<number | null>(null);

  showStudentsModal = signal(false);
  studentsModalLoading = signal(false);
  studentsModalError = signal('');
  selectedClassForStudents = signal<ClassEntity | null>(null);
  studentsBrowserResult = signal<ClassStudentsBrowserResult | null>(null);
  studentsCurrentPage = signal(1);
  studentsItemsPerPage = signal(10);
  studentSearchTerm = '';

  errorMessage = signal('');
  successMessage = signal('');

  newClass: Partial<ClassEntity> = { name: '', gradeLevelId: 0, academicYearId: 0 };

  classStudents = computed<ClassStudentBrowserItem[]>(() => {
    return this.studentsBrowserResult()?.students.items ?? [];
  });

  totalStudentPages = computed(() => {
    return Math.max(1, this.studentsBrowserResult()?.students.totalPages ?? 1);
  });

  ngOnInit() {
    this.loadAcademicYears();
    this.loadGradeLevels();
  }

  loadAcademicYears() {
    this.academicYearService.getAll().subscribe({
      next: (data) => {
        const d = data.data ?? data;
        this.academicYears.set(d);
        const activeYear = d.find((y: any) => y.isCurrent);
        if (activeYear) {
          this.newClass.academicYearId = activeYear.id;
          this.selectedYearFilter = activeYear.id;
        }
        this.loadClasses();
      },
      error: (err) => {
        console.error('Failed to load academic years', err);
        this.showError('فشل في تحميل السنوات الدراسية.');
        this.loadClasses();
      }
    });
  }

  loadGradeLevels() {
    this.gradeLevelService.getAll().subscribe({
      next: (data) => this.gradeLevels.set(data.data ?? data),
      error: (err) => {
        console.error('Failed to load grade levels', err);
        this.showError('فشل في تحميل المراحل الدراسية.');
      }
    });
  }

  loadClasses() {
    const filter: any = {};
    if (this.selectedGradeFilter) filter.gradeLevelId = this.selectedGradeFilter;
    if (this.selectedYearFilter) filter.academicYearId = this.selectedYearFilter;

    this.classService.getAll(filter).subscribe({
      next: (data) => {
        this.classes.set(data.data ?? data);
        this.currentPage.set(1);
      },
      error: (err) => {
        console.error('Failed to load classes', err);
        this.showError('فشل في تحميل الفصول الدراسية.');
      }
    });
  }

  onFilterChange() {
    this.loadClasses();
  }

  editClass(cls: ClassEntity) {
    this.editingClassId.set(cls.id);
    this.newClass = {
      name: cls.name,
      gradeLevelId: cls.gradeLevelId,
      academicYearId: cls.academicYearId
    };
  }

  cancelEdit() {
    this.editingClassId.set(null);
    const yearId = this.selectedYearFilter
      ?? this.academicYears().find(y => y.isCurrent)?.id
      ?? 0;
    this.newClass = { name: '', gradeLevelId: 0, academicYearId: yearId };
  }

  saveClass() {
    if (!this.newClass.name?.trim() || !this.newClass.gradeLevelId || !this.newClass.academicYearId) return;

    if (this.editingClassId()) {
      const { name, gradeLevelId, academicYearId } = this.newClass;
      this.classService.update(this.editingClassId()!, { name, gradeLevelId, academicYearId }).subscribe({
        next: () => {
          this.loadClasses();
          this.cancelEdit();
          this.showSuccess('تم تحديث الفصل بنجاح!');
        },
        error: (err) => {
          console.error('Update failed', err);
          this.showError('فشل في تحديث الفصل. حاول مرة أخرى.');
        }
      });
    } else {
      this.classService.create({ name: this.newClass.name, gradeLevelId: this.newClass.gradeLevelId, academicYearId: this.newClass.academicYearId }).subscribe({
        next: () => {
          this.loadClasses();
          this.cancelEdit();
          this.showSuccess('تم إضافة الفصل بنجاح!');
        },
        error: (err) => {
          console.error('Create failed', err);
          this.showError('فشل في إضافة الفصل. حاول مرة أخرى.');
        }
      });
    }
  }

  deleteClass(id: number) {
    if (confirm('هل أنت متأكد من حذف هذا الفصل؟ قد يؤدي هذا إلى حذف بيانات مرتبطة به.')) {
      this.classService.delete(id).subscribe({
        next: () => {
          this.loadClasses();
          this.showSuccess('تم حذف الفصل بنجاح!');
        },
        error: (err) => {
          console.error('Delete failed', err);
          this.showError('فشل في حذف الفصل. حاول مرة أخرى.');
        }
      });
    }
  }

  openStudentsModal(cls: ClassEntity) {
    this.selectedClassForStudents.set(cls);
    this.showStudentsModal.set(true);
    this.studentsModalError.set('');
    this.studentsBrowserResult.set(null);
    this.studentSearchTerm = '';
    this.studentsCurrentPage.set(1);
    this.studentsItemsPerPage.set(10);
    this.loadClassStudents();
  }

  loadClassStudents() {
    const selectedClass = this.selectedClassForStudents();
    if (!selectedClass) return;

    this.studentsModalLoading.set(true);
    this.studentsModalError.set('');

    this.classStudentsBrowserService.getClassStudents(selectedClass.id, {
      academicYearId: selectedClass.academicYearId,
      page: this.studentsCurrentPage(),
      pageSize: this.studentsItemsPerPage(),
      searchTerm: this.studentSearchTerm
    }).subscribe({
      next: (result) => {
        this.studentsBrowserResult.set(result);
        this.studentsCurrentPage.set(result.students.page);
        this.studentsItemsPerPage.set(result.students.pageSize);
        this.studentsModalLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load class students', err);
        this.studentsModalError.set('فشل في تحميل طلاب الفصل. حاول مرة أخرى.');
        this.studentsModalLoading.set(false);
      }
    });
  }

  closeStudentsModal() {
    this.showStudentsModal.set(false);
    this.studentsModalLoading.set(false);
    this.studentsModalError.set('');
    this.selectedClassForStudents.set(null);
    this.studentsBrowserResult.set(null);
    this.studentSearchTerm = '';
    this.studentsCurrentPage.set(1);
    this.studentsItemsPerPage.set(10);
  }

  applyStudentSearch() {
    this.studentsCurrentPage.set(1);
    this.loadClassStudents();
  }

  clearStudentSearch() {
    this.studentSearchTerm = '';
    this.studentsCurrentPage.set(1);
    this.loadClassStudents();
  }

  onStudentsPageSizeChange() {
    this.studentsCurrentPage.set(1);
    this.loadClassStudents();
  }

  nextStudentsPage() {
    if (this.studentsCurrentPage() < this.totalStudentPages()) {
      this.studentsCurrentPage.update(page => page + 1);
      this.loadClassStudents();
    }
  }

  prevStudentsPage() {
    if (this.studentsCurrentPage() > 1) {
      this.studentsCurrentPage.update(page => page - 1);
      this.loadClassStudents();
    }
  }

  goToStudentsPage(page: number) {
    if (page < 1 || page > this.totalStudentPages() || page === this.studentsCurrentPage()) {
      return;
    }

    this.studentsCurrentPage.set(page);
    this.loadClassStudents();
  }

  getStudentsRangeStart(): number {
    const totalCount = this.studentsBrowserResult()?.students.totalCount ?? 0;
    if (totalCount === 0) return 0;

    return (this.studentsCurrentPage() - 1) * this.studentsItemsPerPage() + 1;
  }

  getStudentsRangeEnd(): number {
    const totalCount = this.studentsBrowserResult()?.students.totalCount ?? 0;
    return Math.min(this.studentsCurrentPage() * this.studentsItemsPerPage(), totalCount);
  }

  getStudentsTotalCount(): number {
    return this.studentsBrowserResult()?.totalStudents ?? 0;
  }

  getStudentsFilteredCount(): number {
    return this.studentsBrowserResult()?.filteredStudentsCount ?? 0;
  }

  hasStudentSearch(): boolean {
    return this.studentSearchTerm.trim().length > 0;
  }

  getGenderLabel(gender?: number | null): string {
    if (gender === 1) return 'ذكر';
    if (gender === 2) return 'أنثى';
    return 'غير محدد';
  }

  formatDisplayDate(dateValue?: string | null): string {
    if (!dateValue) return '—';

    const date = new Date(dateValue);
    if (isNaN(date.getTime())) return '—';

    return new Intl.DateTimeFormat('ar-EG', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(date);
  }

  private showError(msg: string) {
    this.errorMessage.set(msg);
    this.successMessage.set('');
    setTimeout(() => this.errorMessage.set(''), 4000);
  }

  private showSuccess(msg: string) {
    this.successMessage.set(msg);
    this.errorMessage.set('');
    setTimeout(() => this.successMessage.set(''), 3000);
  }
}

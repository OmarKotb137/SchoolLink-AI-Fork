import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ClassService, ClassEntity } from '../../core/services/class.service';
import { GradeLevelService, GradeLevel } from '../../core/services/grade-level.service';
import { AcademicYearService, AcademicYear } from '../../core/services/academic-year.service';

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

  // FIX #1: كانوا signals — [(ngModel)] بتبطل تشتغل مع signals
  // الحل: plain properties عادية
  selectedGradeFilter: number | null = null;
  selectedYearFilter: number | null = null;

  editingClassId = signal<number | null>(null);

  errorMessage = signal('');
  successMessage = signal('');

  newClass: Partial<ClassEntity> = { name: '', gradeLevelId: 0, academicYearId: 0 };

  ngOnInit() {
    // FIX #2: loadClasses كانت بتتنادى مرتين — مرة هنا ومرة جوا loadAcademicYears
    // الحل: شيل الاستدعاء المباشر، خلي loadAcademicYears هي اللي تبدأ loadClasses
    // بعد ما تضبط السنة الحالية
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
          // FIX #2: هنا بس نستدعي loadClasses بعد ما السنة اتضبطت
          this.selectedYearFilter = activeYear.id;
        }
        this.loadClasses();
      },
      error: (err) => {
        console.error('Failed to load academic years', err);
        this.showError('فشل في تحميل السنوات الدراسية.');
        // حتى لو فشل، حمّل الفصول من غير فلتر
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
    // FIX #1: دلوقتي plain properties، مش signals
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
    // نجهز فقط الحقول القابلة للتعديل بدل نسخ خصائص العرض الإضافية،
    // والخدمة تضيف `id` تلقائيا عند تنفيذ التحديث.
    this.newClass = {
      name: cls.name,
      gradeLevelId: cls.gradeLevelId,
      academicYearId: cls.academicYearId
    };
  }

  cancelEdit() {
    this.editingClassId.set(null);
    // FIX: بدل ما نرجع للسنة الحالية دايماً، نرجع للسنة المفلترة حالياً
    // لو مفيش فلتر، نرجع للسنة الحالية
    const yearId = this.selectedYearFilter
      ?? this.academicYears().find(y => y.isCurrent)?.id
      ?? 0;
    this.newClass = { name: '', gradeLevelId: 0, academicYearId: yearId };
  }

  saveClass() {
    if (!this.newClass.name?.trim() || !this.newClass.gradeLevelId || !this.newClass.academicYearId) return;

    if (this.editingClassId()) {
      // نمرر الحقول القابلة للتعديل فقط، والخدمة تضيف `id` تلقائيا.
      const { name, gradeLevelId, academicYearId } = this.newClass;
      this.classService.update(this.editingClassId()!, { name, gradeLevelId, academicYearId }).subscribe({
        next: () => {
          this.loadClasses();
          this.cancelEdit();
          this.showSuccess('تم تحديث الفصل بنجاح!');
        },
        // FIX #4: error handler
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
        // FIX #4: error handler
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
        // FIX #4: error handler
        error: (err) => {
          console.error('Delete failed', err);
          this.showError('فشل في حذف الفصل. حاول مرة أخرى.');
        }
      });
    }
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

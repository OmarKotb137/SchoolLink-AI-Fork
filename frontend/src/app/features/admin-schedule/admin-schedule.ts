import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import {
  TimetableService,
  TimetableListItem,
  TimetableDto as Timetable,
  TimetableValidationResult,
  TimetableValidationIssue
} from '../../core/services/timetable.service';
import { ClassService, ClassEntity } from '../../core/services/class.service';
import { AcademicYearService, AcademicYear } from '../../core/services/academic-year.service';
import { ClassSubjectTeacherService, ClassSubjectTeacher } from '../../core/services/class-subject-teacher.service';
import { RoomService, Room } from '../../core/services/room.service';
import { SubjectService, Subject } from '../../core/services/subject.service';
import { UserService, User } from '../../core/services/user.service';
import { GradeLevelService, GradeLevel } from '../../core/services/grade-level.service';

@Component({
  selector: 'app-admin-schedule',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './admin-schedule.html',
  styleUrl: './admin-schedule.css',
})
export class AdminSchedule implements OnInit, OnDestroy {
  sidebarOpen = signal(false);
  assignmentBankOpen = signal<boolean>(false);
  timetablesPickerOpen = signal(false);
  timetablesFilter = signal<'all' | 'active' | 'draft'>('all');

  private timetableService = inject(TimetableService);
  private classService = inject(ClassService);
  private academicYearService = inject(AcademicYearService);
  private assignmentService = inject(ClassSubjectTeacherService);
  private roomService = inject(RoomService);
  private subjectService = inject(SubjectService);
  private userService = inject(UserService);
  private gradeLevelService = inject(GradeLevelService);

  academicYears = signal<AcademicYear[]>([]);
  classes = signal<ClassEntity[]>([]);
  grades = signal<GradeLevel[]>([]);
  subjects = signal<Subject[]>([]);
  teachers = signal<User[]>([]);
  allTimetables = signal<Timetable[]>([]);
  selectedTimetableId = signal<number | null>(null);

  selectedYearId: number | null = null;
  selectedGradeId: number | null = null;
  selectedClassId: number | null = null;
  filterClasses = signal<ClassEntity[]>([]);

  assignmentSubjectFilter = '';
  assignmentTeacherFilter = '';
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'المشرف';

  currentTimetable = signal<Timetable | null>(null);
  assignments = signal<ClassSubjectTeacher[]>([]);
  availableRooms = signal<Room[]>([]);

  // O(1) assignment lookup map — rebuilt after every loadAssignments()
  private assignmentMap = new Map<number, ClassSubjectTeacher>();

  // ── Loading states ────────────────────────────────────────────────────────
  isLoading        = signal(false);
  isSaving         = signal(false);
  isPublishing     = signal(false);
  isCloning        = signal(false);
  isValidating     = signal(false);
  isDeletingTimetable = signal(false);
  isDeactivating   = signal(false);

  errorMessage   = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  validationResult = signal<TimetableValidationResult | null>(null);

  // ── Inline confirmation dialogs (replace window.confirm) ─────────────────
  deleteSlotConfirmOpen      = signal(false);
  deleteTimetableConfirmOpen = signal(false);
  deactivateConfirmOpen      = signal(false);

  days = [
    { value: 'Sunday',    label: 'الأحد' },
    { value: 'Monday',    label: 'الإثنين' },
    { value: 'Tuesday',   label: 'الثلاثاء' },
    { value: 'Wednesday', label: 'الأربعاء' },
    { value: 'Thursday',  label: 'الخميس' }
  ];

  periods = [
    { num: 1, label: 'الحصة الأولى',   start: '08:00:00', end: '08:45:00' },
    { num: 2, label: 'الحصة الثانية',  start: '08:45:00', end: '09:30:00' },
    { num: 3, label: 'الحصة الثالثة',  start: '09:30:00', end: '10:15:00' },
    { num: 4, label: 'الحصة الرابعة',  start: '10:30:00', end: '11:15:00' },
    { num: 5, label: 'الحصة الخامسة',  start: '11:15:00', end: '12:00:00' },
    { num: 6, label: 'الحصة السادسة',  start: '12:00:00', end: '12:45:00' },
    { num: 7, label: 'الحصة السابعة',  start: '12:45:00', end: '13:30:00' }
  ];

  scheduleEditorOpen = signal(false);

  isModalOpen        = signal(false);
  isEditingSlot      = signal(false);
  isReadOnlyModal    = signal(false);
  editingSlotId      = signal<number | null>(null);
  selectedInspection = signal<{ dayValue: string; periodNum: number; slot: any | null } | null>(null);
  slotFormError      = signal<string | null>(null);

  newSlot = {
    timetableId: 0,
    dayOfWeek: '',
    periodNumber: 0,
    startTime: '',
    endTime: '',
    classSubjectTeacherId: null as number | null,
    isBreak: false,
    roomId: null as number | null
  };

  // ══════════════════════════════════════════════════════════════════════════
  // Lifecycle
  // ══════════════════════════════════════════════════════════════════════════

  ngOnInit() {
    this.loadInitialData();
    try {
      const onResize = () => {
        if (window.innerWidth < 1024 && this.assignmentBankOpen()) {
          this.assignmentBankOpen.set(false);
        }
      };
      window.addEventListener('resize', onResize);
      (this as any)._assignmentBankResizeHandler = onResize;
    } catch { /* ignore SSR / restricted environments */ }
  }

  ngOnDestroy() {
    try {
      const h = (this as any)._assignmentBankResizeHandler;
      if (h) window.removeEventListener('resize', h);
    } catch { }
  }

  // ══════════════════════════════════════════════════════════════════════════
  // Assignment Bank helpers
  // ══════════════════════════════════════════════════════════════════════════

  isAssignmentBankVisible(): boolean { return this.assignmentBankOpen(); }

  toggleAssignmentBank(): void { this.assignmentBankOpen.update(v => !v); }

  toggleTimetablesPicker(): void { this.timetablesPickerOpen.update(v => !v); }

  closeTimetablesPicker(): void { this.timetablesPickerOpen.set(false); }

  setTimetablesFilter(filter: 'all' | 'active' | 'draft'): void {
    this.timetablesFilter.set(filter);
  }

  selectTimetable(timetableId: number): void {
    const timetable = this.allTimetables().find(item => item.id === timetableId) || null;
    if (!timetable) return;

    this.selectedTimetableId.set(timetable.id);
    this.currentTimetable.set(timetable);
    this.validationResult.set(null);
    this.selectedInspection.set(null);
    this.closeTimetablesPicker();
  }

  // ══════════════════════════════════════════════════════════════════════════
  // Schedule editor helpers
  // ══════════════════════════════════════════════════════════════════════════

  openScheduleEditor(): void {
    if (!this.currentTimetable()) return;
    this.scheduleEditorOpen.set(true);
  }

  closeScheduleEditor(): void { this.scheduleEditorOpen.set(false); }

  getSelectedClassName(): string {
    return this.classes().find(c => c.id === this.selectedClassId)?.name || '';
  }

  // ══════════════════════════════════════════════════════════════════════════
  // Data loading
  // ══════════════════════════════════════════════════════════════════════════

  loadInitialData() {
    this.subjectService.getAll().subscribe({
      next: (data) => this.subjects.set(data),
      error: () => this.showError('تعذر تحميل بيانات المواد')
    });
    this.userService.getByRole('Teacher', 1000).subscribe({
      next: (res) => this.teachers.set(res.items || []),
      error: () => this.showError('تعذر تحميل بيانات المعلمين')
    });
    this.gradeLevelService.getAll().subscribe({
      next: (data) => {
        const sortedGrades = data.sort((a, b) => a.levelOrder - b.levelOrder);
        this.grades.set(sortedGrades);
      },
      error: () => this.showError('تعذر تحميل بيانات الصفوف الدراسية')
    });
    this.classService.getAll().subscribe({
      next: (data) => this.classes.set(data),
      error: () => this.showError('تعذر تحميل بيانات الفصول')
    });
    this.academicYearService.getAll().subscribe({
      next: (data) => {
        this.academicYears.set(data);
        const active = data.find(y => y.isCurrent);
        if (active) this.selectedYearId = active.id;
      },
      error: () => this.showError('تعذر تحميل السنوات الدراسية')
    });
  }

  onGradeFilterChange() {
    this.selectedClassId = null;
    if (this.selectedGradeId) {
      this.filterClasses.set(this.classes().filter(c => c.gradeLevelId === this.selectedGradeId));
    } else {
      this.filterClasses.set([]);
    }
    this.onFilterChange();
  }

  onFilterChange() {
    this.selectedTimetableId.set(null);
    this.closeTimetablesPicker();
    this.closeScheduleEditor();
    this.closeModal();
    if (this.selectedYearId && this.selectedClassId) {
      this.loadTimetables();
      this.loadAssignments();
    } else {
      this.resetTimetableState();
      this.assignments.set([]);
      this.assignmentMap.clear();
    }
  }

  loadAssignments() {
    this.assignmentService.getByClass(this.selectedClassId!, this.selectedYearId!).subscribe({
      next: (data) => {
        this.assignments.set(data);
        // Rebuild Map for O(1) lookups (called 35× per render: 5 days × 7 periods)
        this.assignmentMap = new Map(data.map(a => [a.id!, a]));
      },
      error: () => {
        this.assignments.set([]);
        this.assignmentMap.clear();
        this.showError('تعذر تحميل تعيينات هذا الفصل');
      }
    });
  }

  loadTimetables() {
    this.isLoading.set(true);
    this.timetableService.getByClass(this.selectedClassId!, this.selectedYearId!).subscribe({
      next: (data) => {
        const timetables = (Array.isArray(data) ? data : []).map(item => this.normalizeTimetable(item));
        const selectedTimetableId = this.selectedTimetableId();
        const nextTimetable = timetables.find(item => item.id === selectedTimetableId)
          || this.pickWorkingTimetable(timetables);

        this.allTimetables.set(timetables);
        this.currentTimetable.set(nextTimetable);
        this.selectedTimetableId.set(nextTimetable?.id ?? null);
        this.validationResult.set(null);
        this.selectedInspection.set(null);
        this.isLoading.set(false);
      },
      error: (err: any) => {
        this.resetTimetableState();
        this.isLoading.set(false);
        if (err?.status === 403) {
          this.showError('ليس لديك صلاحية للوصول إلى هذا الجدول.');
          return;
        }
        if (err?.status && err.status !== 404) {
          this.showError(this.getApiErrorMessage(err, 'تعذر تحميل الجداول الدراسية لهذا الفصل.'));
        }
      }
    });
  }

  // ══════════════════════════════════════════════════════════════════════════
  // Timetable CRUD
  // ══════════════════════════════════════════════════════════════════════════

  createTimetable() {
    if (!this.selectedClassId || !this.selectedYearId) return;
    if (this.hasDraftTimetable()) {
      this.showError('توجد بالفعل مسودة مفتوحة لهذا الفصل. أكمل العمل عليها أو فعّلها أولًا.');
      return;
    }
    this.timetableService.create({
      classId: this.selectedClassId,
      academicYearId: this.selectedYearId
    }).subscribe({
      next: () => {
        this.selectedTimetableId.set(null);
        this.loadTimetables();
        this.showSuccess('تم إنشاء مسودة الجدول بنجاح، ويمكنك الآن بدء التسكين بأمان.');
      },
      error: (err: any) => this.showError(this.getApiErrorMessage(err, 'فشل إنشاء مسودة الجدول.'))
    });
  }

  cloneDraftFromCurrent() {
    if (!this.selectedClassId || !this.selectedYearId) return;
    if (this.hasDraftTimetable()) {
      this.showError('توجد بالفعل مسودة حالية. لا يمكن إنشاء نسخة أخرى قبل إنهائها.');
      return;
    }
    this.isCloning.set(true);
    this.timetableService.cloneDraft(this.selectedClassId, this.selectedYearId).subscribe({
      next: () => {
        this.isCloning.set(false);
        this.selectedTimetableId.set(null);
        this.loadTimetables();
        this.showSuccess('تم إنشاء مسودة جديدة بنسخ الجدول الحالي، ويمكنك الآن تعديلها بأمان.');
      },
      error: (err: any) => {
        this.isCloning.set(false);
        this.showError(this.getApiErrorMessage(err, 'تعذر إنشاء مسودة من الجدول الحالي.'));
      }
    });
  }

  // ── Delete draft ──────────────────────────────────────────────────────────

  canDeleteTimetable(): boolean {
    return !!this.currentTimetable() && !this.currentTimetable()!.isActive;
  }

  deleteTimetable() {
    const timetable = this.currentTimetable();
    if (!timetable || timetable.isActive) return;

    this.deleteTimetableConfirmOpen.set(false);
    this.isDeletingTimetable.set(true);
    this.timetableService.delete(timetable.id).subscribe({
      next: () => {
        this.isDeletingTimetable.set(false);
        this.resetTimetableState();
        if (this.selectedClassId && this.selectedYearId) {
          this.loadTimetables();
        }
        this.showSuccess('تم حذف المسودة بنجاح.');
      },
      error: (err: any) => {
        this.isDeletingTimetable.set(false);
        this.showError(this.getApiErrorMessage(err, 'تعذر حذف المسودة الحالية.'));
      }
    });
  }

  // ── Deactivate published timetable ────────────────────────────────────────

  canDeactivateTimetable(): boolean {
    return !!this.currentTimetable() && !!this.currentTimetable()!.isActive;
  }

  deactivateTimetable() {
    const timetable = this.currentTimetable();
    if (!timetable || !timetable.isActive) return;

    this.deactivateConfirmOpen.set(false);
    this.isDeactivating.set(true);
    this.timetableService.deactivate(timetable.id).subscribe({
      next: () => {
        this.isDeactivating.set(false);
        this.loadTimetables();
        this.validationResult.set(null);
        this.showSuccess('تم إلغاء تفعيل الجدول وأصبح مسودة قابلة للتعديل.');
      },
      error: (err: any) => {
        this.isDeactivating.set(false);
        this.showError(this.getApiErrorMessage(err, 'تعذر إلغاء تفعيل الجدول.'));
      }
    });
  }

  // ── Publish ────────────────────────────────────────────────────────────────

  publishCurrentTimetable() {
    const timetable = this.currentTimetable();
    if (!timetable || timetable.isActive || this.hasBlockingValidationErrors()) return;

    // FIX: if validation hasn't been run yet, run it first so the admin
    // sees the issues panel before hitting the backend with a bad request.
    if (!this.validationResult()) {
      this.showError('يُنصح بإجراء المراجعة قبل التفعيل. جارٍ التحقق تلقائيًا…');
      this.validateCurrentTimetable();
      return;
    }

    this.isPublishing.set(true);
    this.timetableService.activate(timetable.id).subscribe({
      next: () => {
        this.isPublishing.set(false);
        this.loadTimetables();
        this.validationResult.set(null);
        this.showSuccess('تم تفعيل الجدول بنجاح وأصبح هو الجدول المنشور.');
      },
      error: (err: any) => {
        this.isPublishing.set(false);
        this.showError(this.getApiErrorMessage(err, 'تعذر تفعيل الجدول الحالي.'));
        // FIX: auto-run validation so admin can see which slots caused the failure
        // without having to press "مراجعة" manually.
        if (!this.validationResult()) {
          this.validateCurrentTimetable();
        }
      }
    });
  }

  // ── Validate ───────────────────────────────────────────────────────────────

  validateCurrentTimetable() {
    const timetable = this.currentTimetable();
    if (!timetable) return;

    this.isValidating.set(true);
    this.timetableService.validate(timetable.id).subscribe({
      next: (result) => {
        this.isValidating.set(false);
        this.validationResult.set(result);
        this.showSuccess(result.canActivate
          ? 'اكتملت المراجعة: الجدول جاهز للتفعيل.'
          : 'اكتملت المراجعة: توجد عناصر تحتاج معالجة قبل التفعيل.');
      },
      error: (err: any) => {
        this.isValidating.set(false);
        this.showError(this.getApiErrorMessage(err, 'تعذر تنفيذ المراجعة على الجدول الحالي.'));
      }
    });
  }

  // ══════════════════════════════════════════════════════════════════════════
  // Slot modal
  // ══════════════════════════════════════════════════════════════════════════

  getSlot(dayValue: string, periodNum: number): any {
    return this.currentTimetable()?.slots?.find((s: any) =>
      s.dayOfWeek === dayValue && s.periodNumber === periodNum
    ) || null;
  }

  /**
   * FIX #1 — openSlotModal was returning early before isReadOnlyModal could be set,
   * so published timetables always showed an error instead of a read-only view.
   *
   * Logic now:
   *   - No timetable selected          → error, return
   *   - Published + empty cell         → do nothing (silently)
   *   - Published + filled cell        → open read-only modal
   *   - Draft + any cell               → open editable modal (add or edit)
   */
  openSlotModal(dayValue: string, periodNum: number) {
    if (!this.currentTimetable()) {
      this.showError('أنشئ أو اختر جدولًا أولًا قبل إدارة الحصص.');
      return;
    }

    const existingSlot = this.getSlot(dayValue, periodNum);
    const periodData   = this.periods.find(p => p.num === periodNum)!;
    this.selectedInspection.set({ dayValue, periodNum, slot: existingSlot });

    const canEdit = this.canEditCurrentTimetable();

    if (!canEdit) {
      // Published timetable: empty cells do nothing; filled cells → read-only
      if (!existingSlot) return;
      this.isReadOnlyModal.set(true);
    } else {
      this.isReadOnlyModal.set(false);
    }

    this.isEditingSlot.set(!!existingSlot);
    this.editingSlotId.set(existingSlot?.id ?? null);
    this.deleteSlotConfirmOpen.set(false);
    this.slotFormError.set(null);

    this.newSlot = {
      timetableId:            this.currentTimetable()!.id,
      dayOfWeek:              dayValue,
      periodNumber:           periodNum,
      startTime:              existingSlot?.startTime  || periodData.start,
      endTime:                existingSlot?.endTime    || periodData.end,
      classSubjectTeacherId:  existingSlot?.classSubjectTeacherId ?? null,
      isBreak:                !!existingSlot?.isBreak,
      roomId:                 existingSlot?.roomId ?? null
    };

    if (this.newSlot.isBreak) {
      this.availableRooms.set([]);
    } else {
      this.loadAvailableRooms(dayValue, periodNum);
    }

    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
    this.isEditingSlot.set(false);
    this.isReadOnlyModal.set(false);
    this.editingSlotId.set(null);
    this.availableRooms.set([]);
    this.deleteSlotConfirmOpen.set(false);
    this.slotFormError.set(null);
  }

  saveSlot() {
    if (!this.currentTimetable()) {
      this.showSlotFormError('لا يوجد جدول محدد للحفظ عليه.');
      return;
    }
    if (this.isReadOnlyModal()) {
      this.showSlotFormError('هذه الحصة معروضة للقراءة فقط لأن الجدول الحالي منشور.');
      return;
    }
    if (!this.canEditCurrentTimetable()) {
      this.showSlotFormError('الجدول الحالي منشور ولا يقبل التعديل المباشر.');
      return;
    }
    if (!this.newSlot.isBreak && !this.newSlot.classSubjectTeacherId) {
      this.showSlotFormError('الرجاء اختيار المادة والمعلم قبل حفظ الحصة.');
      return;
    }
    if (this.newSlot.endTime <= this.newSlot.startTime) {
      this.showSlotFormError('وقت النهاية يجب أن يكون بعد وقت البداية.');
      return;
    }

    const assignmentId = this.newSlot.isBreak ? null : this.newSlot.classSubjectTeacherId;
    const roomId       = this.newSlot.isBreak ? null : this.newSlot.roomId;
    this.slotFormError.set(null);
    this.isSaving.set(true);

    if (this.isEditingSlot() && this.editingSlotId()) {
      this.timetableService.updateSlot(this.editingSlotId()!, {
        slotId:                 this.editingSlotId()!,
        dayOfWeek:              this.newSlot.dayOfWeek,
        periodNumber:           this.newSlot.periodNumber,
        startTime:              this.newSlot.startTime,
        endTime:                this.newSlot.endTime,
        classSubjectTeacherId:  assignmentId,
        isBreak:                this.newSlot.isBreak,
        roomId
      }).subscribe({
        next:  () => this.finishSlotMutation('تم تحديث الحصة بنجاح'),
        error: (err: any) => {
          this.isSaving.set(false);
          this.showSlotFormError(this.getApiErrorMessage(err, 'تعذر تحديث الحصة الحالية.'));
        }
      });
      return;
    }

    this.timetableService.addSlot({
      timetableId:            this.currentTimetable()!.id,
      dayOfWeek:              this.newSlot.dayOfWeek,
      periodNumber:           this.newSlot.periodNumber,
      startTime:              this.newSlot.startTime,
      endTime:                this.newSlot.endTime,
      classSubjectTeacherId:  assignmentId,
      isBreak:                this.newSlot.isBreak,
      roomId
    }).subscribe({
      next:  () => this.finishSlotMutation('تم تسكين الحصة بنجاح'),
      error: (err: any) => {
        this.isSaving.set(false);
        this.showSlotFormError(this.getApiErrorMessage(err, 'تعذر إضافة الحصة. تأكد من عدم وجود تعارض للمعلم أو القاعة.'));
      }
    });
  }

  onBreakToggle() {
    if (this.newSlot.isBreak) {
      this.newSlot.classSubjectTeacherId = null;
      this.newSlot.roomId = null;
      this.availableRooms.set([]);
      return;
    }
    this.loadAvailableRooms(this.newSlot.dayOfWeek, this.newSlot.periodNumber);
  }

  // ── Two-step slot delete (replaces window.confirm) ─────────────────────

  requestDeleteSlot() {
    this.deleteSlotConfirmOpen.set(true);
  }

  cancelDeleteSlot() {
    this.deleteSlotConfirmOpen.set(false);
  }

  deleteEditingSlot() {
    const slotId = this.editingSlotId();
    if (!slotId) return;
    if (!this.canEditCurrentTimetable()) {
      this.showSlotFormError('لا يمكن حذف حصص من جدول منشور.');
      return;
    }

    this.deleteSlotConfirmOpen.set(false);
    this.isSaving.set(true);
    this.timetableService.deleteSlot(slotId).subscribe({
      next:  () => this.finishSlotMutation('تم حذف الحصة بنجاح'),
      error: (err: any) => {
        this.isSaving.set(false);
        this.showSlotFormError(this.getApiErrorMessage(err, 'تعذر حذف الحصة الحالية.'));
      }
    });
  }

  // ══════════════════════════════════════════════════════════════════════════
  // Assignment helpers
  // ══════════════════════════════════════════════════════════════════════════

  // O(1) lookup via Map — falls back to linear scan for safety
  getAssignmentDetails(cstId: number) {
    const assignment = this.assignmentMap.get(cstId) ?? this.assignments().find(a => a.id == cstId);
    if (!assignment) return null;
    return {
      subjectName: assignment.subjectName || this.getSubjectName(assignment.subjectId),
      teacherName: assignment.teacherName || this.getTeacherName(assignment.teacherId)
    };
  }

  getSubjectName(subjectId: number): string {
    return this.subjects().find(s => s.id == subjectId)?.name || 'مجهول';
  }

  getTeacherName(teacherId: number): string {
    return this.teachers().find(t => t.id == teacherId)?.fullName || 'مجهول';
  }

  getRemainingPeriods(assignment: ClassSubjectTeacher): number {
    return Math.max(assignment.weeklyPeriods - this.getUsedPeriods(assignment.id!), 0);
  }

  getAssignmentProgressWidth(assignment: ClassSubjectTeacher): number {
    if (assignment.weeklyPeriods <= 0) return 0;
    const used = Math.min(this.getUsedPeriods(assignment.id!), assignment.weeklyPeriods);
    return Math.max(0, Math.min(100, (used / assignment.weeklyPeriods) * 100));
  }

  isAssignmentSelectable(assignment: ClassSubjectTeacher): boolean {
    const remaining = this.getRemainingPeriods(assignment);
    return remaining > 0 || this.getEditingSlot()?.classSubjectTeacherId == assignment.id;
  }

  filteredAssignments(): ClassSubjectTeacher[] {
    const subjectFilter = this.assignmentSubjectFilter.trim().toLowerCase();
    const teacherFilter = this.assignmentTeacherFilter.trim().toLowerCase();

    return this.assignments().filter(assign => {
      const subjectName = (assign.subjectName || this.getSubjectName(assign.subjectId)).toLowerCase();
      const teacherName = (assign.teacherName || this.getTeacherName(assign.teacherId)).toLowerCase();
      return (!subjectFilter || subjectName.includes(subjectFilter))
          && (!teacherFilter || teacherName.includes(teacherFilter));
    });
  }

  // ══════════════════════════════════════════════════════════════════════════
  // State predicates
  // ══════════════════════════════════════════════════════════════════════════

  hasDraftTimetable(): boolean      { return this.allTimetables().some(t => !t.isActive); }
  hasPublishedTimetable(): boolean  { return this.allTimetables().some(t =>  t.isActive); }
  hasMultipleTimetables(): boolean  { return this.allTimetables().length > 1; }
  canEditCurrentTimetable(): boolean { return !!this.currentTimetable() && !this.currentTimetable()!.isActive; }
  canCreateDraft(): boolean  { return !!this.selectedClassId && !!this.selectedYearId && !this.hasDraftTimetable(); }
  canCloneDraft(): boolean   { return !!this.selectedClassId && !!this.selectedYearId && !this.hasDraftTimetable() && this.allTimetables().length > 0; }
  hasBlockingValidationErrors(): boolean { return !!this.validationResult()?.errors?.length; }

  // ══════════════════════════════════════════════════════════════════════════
  // UI label / display helpers
  // ══════════════════════════════════════════════════════════════════════════

  getCurrentStatusLabel(): string {
    const t = this.currentTimetable();
    return t ? (t.isActive ? 'منشور' : 'مسودة') : '';
  }

  getTimetableCountLabel(): string {
    const total = this.allTimetables().length;
    return total === 0 ? 'لا توجد نسخ' : `${total} ${total === 1 ? 'نسخة' : 'نسخ'}`;
  }

  getFilteredTimetableCountLabel(): string {
    const total = this.filteredTimetables().length;
    return total === 0 ? 'لا توجد نتائج' : `${total} ${total === 1 ? 'نسخة' : 'نسخ'}`;
  }

  getCurrentTimetableVersionLabel(): string {
    const currentId = this.currentTimetable()?.id;
    const index = this.allTimetables().findIndex(t => t.id === currentId);
    return index >= 0 ? `النسخة ${index + 1}` : 'لم يتم اختيار نسخة';
  }

  isSelectedTimetable(timetableId: number): boolean {
    return this.selectedTimetableId() === timetableId;
  }

  isTimetablesFilterActive(filter: 'all' | 'active' | 'draft'): boolean {
    return this.timetablesFilter() === filter;
  }

  getPublishedTimetablesCount(): number {
    return this.allTimetables().filter(t => t.isActive).length;
  }

  getDraftTimetablesCount(): number {
    return this.allTimetables().filter(t => !t.isActive).length;
  }

  filteredTimetables(): Timetable[] {
    const filter = this.timetablesFilter();
    if (filter === 'active') return this.allTimetables().filter(t => t.isActive);
    if (filter === 'draft') return this.allTimetables().filter(t => !t.isActive);
    return this.allTimetables();
  }

  formatTimetableDate(value: string | null | undefined): string {
    if (!value) return 'غير متاح';

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) return 'غير متاح';

    return new Intl.DateTimeFormat('ar-EG', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    }).format(parsed);
  }

  formatSlotTimeRange(startTime: string | null | undefined, endTime: string | null | undefined): string {
    if (!startTime || !endTime) return 'الوقت غير محدد';
    return `${startTime.slice(0, 5)} - ${endTime.slice(0, 5)}`;
  }

  getCurrentStatusMessage(): string {
    const t = this.currentTimetable();
    if (!t) return '';
    return t.isActive
      ? 'هذه هي النسخة المنشورة حاليًا. يمكنك فتح نسخة أخرى من قائمة الجداول أو إلغاء التفعيل إذا أردت تحويلها لمسودة.'
      : 'أنت تعمل الآن على مسودة مستقلة. يمكنك تعديلها بأمان ثم تفعيلها عند الانتهاء.';
  }

  getValidationHeadline(): string {
    const r = this.validationResult();
    if (!r) return '';
    return r.canActivate ? 'نتيجة المراجعة: الجدول جاهز للتفعيل' : 'نتيجة المراجعة: توجد مشكلات تحتاج معالجة';
  }

  getValidationTone(): string {
    const r = this.validationResult();
    if (!r) return '';
    return r.canActivate
      ? 'bg-green-50 text-green-800 border-green-200'
      : 'bg-amber-50 text-amber-900 border-amber-200';
  }

  getValidationSummary(): string {
    const r = this.validationResult();
    if (!r) return '';
    return `إجمالي الخلايا: ${r.totalSlots}، الحصص الدراسية: ${r.lessonSlots}، فترات الراحة: ${r.breakSlots}، الأخطاء: ${r.errors.length}، التحذيرات: ${r.warnings.length}`;
  }

  getCompletionPercent(): number {
    const assignments = this.assignments();
    if (!assignments.length) return 0;
    const totalRequired = assignments.reduce((s, a) => s + a.weeklyPeriods, 0);
    if (totalRequired <= 0) return 0;
    const totalScheduled = assignments.reduce((s, a) => s + Math.min(this.getUsedPeriods(a.id!), a.weeklyPeriods), 0);
    return Math.max(0, Math.min(100, Math.round((totalScheduled / totalRequired) * 100)));
  }

  getOutstandingAssignmentsCount(): number {
    return this.assignments().filter(a => this.getRemainingPeriods(a) > 0).length;
  }

  getDaySummary(dayValue: string) {
    const slots = (this.currentTimetable()?.slots || []).filter((s: any) => s.dayOfWeek === dayValue);
    return {
      filled:  slots.length,
      lessons: slots.filter((s: any) => !s.isBreak).length,
      breaks:  slots.filter((s: any) =>  s.isBreak).length,
      issues:  slots.reduce((n: number, s: any) => n + this.getSlotIssueCount(s), 0)
    };
  }

  getDaySummaryTone(dayValue: string): string {
    const { issues, filled } = this.getDaySummary(dayValue);
    if (issues > 0) return 'bg-amber-50 border-amber-200 text-amber-900';
    if (filled  === 0) return 'bg-surface-container-low border-outline-variant/30 text-on-surface-variant';
    return 'bg-primary/5 border-primary/15 text-primary';
  }

  // ══════════════════════════════════════════════════════════════════════════
  // Cell / slot display helpers
  // ══════════════════════════════════════════════════════════════════════════

  getCellClass(dayValue: string, periodNum: number): string {
    const slot = this.getSlot(dayValue, periodNum);
    if (!slot) return this.canEditCurrentTimetable() ? 'hover:bg-primary/5' : '';
    if (this.hasSlotError(slot))   return 'bg-red-50 ring-1 ring-inset ring-red-200';
    if (this.hasSlotWarning(slot)) return 'bg-amber-50 ring-1 ring-inset ring-amber-200';
    return 'bg-primary/5';
  }

  // FIX: cursor — published+filled = pointer (can view), published+empty = default, draft = always pointer
  getCellCursor(dayValue: string, periodNum: number): string {
    if (this.canEditCurrentTimetable()) return 'cursor-pointer';
    return this.getSlot(dayValue, periodNum) ? 'cursor-pointer' : 'cursor-default';
  }

  getSlotIssueCount(slot: any): number   { return this.getSlotIssues(slot).length; }
  getSlotIssueIcon(slot: any):  string   { return this.hasSlotError(slot) ? 'error' : 'warning'; }
  hasSlotError(slot: any):      boolean  { return this.getSlotIssues(slot).some(i => i.severity.toLowerCase() === 'error'); }
  hasSlotWarning(slot: any):    boolean  { return this.getSlotIssues(slot).some(i => i.severity.toLowerCase() === 'warning'); }

  getSlotIssueTooltip(slot: any): string {
    return this.getSlotIssues(slot).slice(0, 2).map(i => i.message).join(' | ');
  }

  // ══════════════════════════════════════════════════════════════════════════
  // Inspection panel helpers
  // ══════════════════════════════════════════════════════════════════════════

  getSelectedInspectionTitle(): string {
    const sel = this.selectedInspection();
    if (!sel) return 'اختر خلية من الجدول';
    const dayLabel    = this.getDayLabel(sel.dayValue);
    const periodLabel = this.periods.find(p => p.num === sel.periodNum)?.label || `الحصة ${sel.periodNum}`;
    return `${dayLabel} - ${periodLabel}`;
  }

  getSelectedInspectionSubtitle(): string {
    const sel = this.selectedInspection();
    if (!sel?.slot) return 'يمكنك الضغط على أي خلية لعرض حالتها ومشكلاتها المرتبطة.';
    if (sel.slot.isBreak) return 'الفترة الحالية مسجلة كفترة راحة.';
    const details = this.getAssignmentDetails(sel.slot.classSubjectTeacherId);
    return `${details?.subjectName || 'حصة دراسية'}${details?.teacherName ? ` - ${details.teacherName}` : ''}`;
  }

  getSelectedInspectionIssues(): TimetableValidationIssue[] {
    const sel = this.selectedInspection();
    if (!sel) return [];
    return this.resolveIssuesForContext(sel.slot, sel.dayValue, sel.periodNum);
  }

  hasSelectedInspectionIssues(): boolean { return this.getSelectedInspectionIssues().length > 0; }

  formatValidationIssue(issue: TimetableValidationIssue): string {
    const parts: string[] = [];
    if (issue.dayOfWeek)    parts.push(this.getDayLabel(issue.dayOfWeek));
    if (issue.periodNumber) parts.push(`الحصة ${issue.periodNumber}`);
    return parts.length ? `${parts.join(' - ')}: ${issue.message}` : issue.message;
  }

  getDayLabel(dayValue: string): string {
    return this.days.find(d => d.value === dayValue)?.label || dayValue;
  }

  // ══════════════════════════════════════════════════════════════════════════
  // Private helpers
  // ══════════════════════════════════════════════════════════════════════════

  private pickWorkingTimetable(timetables: Timetable[]): Timetable | null {
    if (!timetables.length) return null;
    return timetables.find(t => !t.isActive) || timetables.find(t => t.isActive) || null;
  }

  private normalizeTimetable(item: Timetable | TimetableListItem): Timetable {
    return {
      id:             item.id,
      classId:        Number(item.classId),
      className:      item.className ?? '',
      academicYearId: Number(item.academicYearId),
      isActive:       !!item.isActive,
      createdAt:      item.createdAt ?? '',
      updatedAt:      item.updatedAt ?? item.createdAt ?? '',
      slots:          item.slots ?? [],
    };
  }

  private resetTimetableState() {
    this.allTimetables.set([]);
    this.selectedTimetableId.set(null);
    this.timetablesFilter.set('all');
    this.currentTimetable.set(null);
    this.validationResult.set(null);
    this.selectedInspection.set(null);
    this.deleteTimetableConfirmOpen.set(false);
    this.deactivateConfirmOpen.set(false);
    this.timetablesPickerOpen.set(false);
  }

  private loadAvailableRooms(dayValue: string, periodNum: number) {
    this.roomService.getAvailable(dayValue, periodNum).subscribe({
      next: (data: Room[]) => this.availableRooms.set(data),
      error: () => this.availableRooms.set([])
    });
  }

  private getUsedPeriods(assignmentId: number): number {
    return (this.currentTimetable()?.slots || []).filter((s: any) =>
      !s.isBreak && s.classSubjectTeacherId == assignmentId
    ).length;
  }

  private getSlotIssues(slot: any): TimetableValidationIssue[] {
    if (!slot) return [];
    return this.resolveIssuesForContext(slot, slot.dayOfWeek, slot.periodNumber);
  }

  private resolveIssuesForContext(slot: any | null, dayValue: string, periodNum: number): TimetableValidationIssue[] {
    const validation = this.validationResult();
    if (!validation) return [];
    return [...validation.errors, ...validation.warnings].filter(issue =>
      (!!slot && issue.slotId === slot.id) ||
      (!!issue.dayOfWeek && issue.dayOfWeek === dayValue && issue.periodNumber === periodNum) ||
      (!!slot && !!issue.classSubjectTeacherId && issue.classSubjectTeacherId === slot.classSubjectTeacherId)
    );
  }

  private getEditingSlot(): any | null {
    const slotId = this.editingSlotId();
    if (!slotId) return null;
    return (this.currentTimetable()?.slots || []).find((s: any) => s.id === slotId) || null;
  }

  private finishSlotMutation(message: string) {
    this.isSaving.set(false);
    this.slotFormError.set(null);
    this.closeModal();
    this.loadTimetables();
    this.showSuccess(message);
  }

  private showSlotFormError(message: string) {
    this.slotFormError.set(message);
  }

  private getApiErrorMessage(err: any, fallback: string): string {
    const message = err?.error?.message || err?.error;
    return typeof message === 'string' && message.trim() ? message : fallback;
  }

  private showError(msg: string) {
    this.errorMessage.set(msg);
    this.successMessage.set(null);
    setTimeout(() => this.errorMessage.set(null), 5000);
  }

  private showSuccess(msg: string) {
    this.successMessage.set(msg);
    this.errorMessage.set(null);
    setTimeout(() => this.successMessage.set(null), 3000);
  }
}

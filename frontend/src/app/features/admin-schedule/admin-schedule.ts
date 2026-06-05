import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { TimetableService, Timetable } from '../../core/services/timetable.service';
import { ClassService, ClassEntity } from '../../core/services/class.service';
import { AcademicYearService, AcademicYear } from '../../core/services/academic-year.service';
import { ClassSubjectTeacherService, ClassSubjectTeacher } from '../../core/services/class-subject-teacher.service';
import { RoomService, Room } from '../../core/services/room.service';
import { SubjectService, Subject } from '../../core/services/subject.service';
import { UserService, User } from '../../core/services/user.service';

@Component({
  selector: 'app-admin-schedule',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './admin-schedule.html',
  styleUrl: './admin-schedule.css',
})
export class AdminSchedule implements OnInit {
  sidebarOpen = signal(false);

  private timetableService = inject(TimetableService);
  private classService = inject(ClassService);
  private academicYearService = inject(AcademicYearService);
  private assignmentService = inject(ClassSubjectTeacherService);
  private roomService = inject(RoomService);
  private subjectService = inject(SubjectService);
  private userService = inject(UserService);

  academicYears = signal<AcademicYear[]>([]);
  classes = signal<ClassEntity[]>([]);
  subjects = signal<Subject[]>([]);
  teachers = signal<User[]>([]);

  // FIX 1: plain properties instead of signals — [(ngModel)] needs a writable plain value
  selectedYearId: number | null = null;
  selectedClassId: number | null = null;

  activeTimetable = signal<Timetable | null>(null);
  assignments = signal<ClassSubjectTeacher[]>([]);
  availableRooms = signal<Room[]>([]);

  // UI state
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

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

  isModalOpen = signal(false);
  selectedDay = signal('');
  selectedPeriod = signal(0);

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

  ngOnInit() {
    this.loadInitialData();
  }

  loadInitialData() {
    // FIX 2: added error handlers to all service calls
    this.subjectService.getAll().subscribe({
      next: (data) => this.subjects.set(data),
      error: () => this.showError('تعذر تحميل بيانات المواد')
    });
    // FIX 3: pass pageSize=1000 so all teachers are returned (not just page 1)
    this.userService.getByRole('Teacher', 1000).subscribe({
      next: (res) => this.teachers.set(res.items || []),
      error: () => this.showError('تعذر تحميل بيانات المعلمين')
    });
    this.classService.getAll().subscribe({
      next: (data) => this.classes.set(data),
      error: () => this.showError('تعذر تحميل بيانات الفصول')
    });
    this.academicYearService.getAll().subscribe({
      next: (data) => {
        this.academicYears.set(data);
        const active = data.find(y => y.isCurrent);
        // FIX 4: set plain property, not signal
        if (active) this.selectedYearId = active.id;
      },
      error: () => this.showError('تعذر تحميل السنوات الدراسية')
    });
  }

  onFilterChange() {
    // FIX 5: use plain properties (no () call)
    if (this.selectedYearId && this.selectedClassId) {
      this.loadTimetable();
      this.loadAssignments();
    } else {
      this.activeTimetable.set(null);
      this.assignments.set([]);
    }
  }

  loadAssignments() {
    this.assignmentService.getByClass(this.selectedClassId!, this.selectedYearId!).subscribe({
      next: (data) => this.assignments.set(data),
      // FIX 6: add error handler
      error: () => this.showError('تعذر تحميل تعيينات هذا الفصل')
    });
  }

  loadTimetable() {
    this.isLoading.set(true);
    this.timetableService.getActiveByClass(this.selectedClassId!, this.selectedYearId!).subscribe({
      next: (data) => {
        this.activeTimetable.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.activeTimetable.set(null);
        this.isLoading.set(false);
      }
    });
  }

  createTimetable() {
    if (!this.selectedClassId || !this.selectedYearId) return;
    this.timetableService.create({
      classId: this.selectedClassId,
      academicYearId: this.selectedYearId
    }).subscribe({
      next: () => {
        this.loadTimetable();
        this.showSuccess('تم إنشاء الجدول بنجاح، يمكنك الآن تسكين الحصص');
      },
      // FIX 7: replace alert() with inline error banner
      error: (err: any) => this.showError('فشل إنشاء الجدول: ' + (err?.error?.message || err?.error || 'ربما يوجد جدول بالفعل لهذا الفصل'))
    });
  }

  getSlot(dayValue: string, periodNum: number): any {
    if (!this.activeTimetable() || !this.activeTimetable()!.slots) return null;
    return this.activeTimetable()!.slots!.find((s: any) => s.dayOfWeek === dayValue && s.periodNumber === periodNum);
  }

  // FIX 8: use DTO name fields (subjectName / teacherName) before falling back to local lookups
  getAssignmentDetails(cstId: number) {
    const assignment = this.assignments().find(a => a.id == cstId);
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

  openSlotModal(dayValue: string, periodNum: number) {
    const existingSlot = this.getSlot(dayValue, periodNum);
    if (existingSlot) {
      if (confirm('توجد حصة بالفعل هنا. هل تريد حذفها لإضافة واحدة جديدة؟')) {
        this.timetableService.deleteSlot(existingSlot.id).subscribe({
          next: () => {
            this.loadTimetable();
            this.showSuccess('تم حذف الحصة بنجاح');
          },
          // FIX 9: add error handler for deleteSlot
          error: () => this.showError('تعذر حذف الحصة')
        });
      }
      return;
    }

    this.selectedDay.set(dayValue);
    this.selectedPeriod.set(periodNum);
    const periodData = this.periods.find(p => p.num === periodNum)!;

    this.newSlot = {
      timetableId: this.activeTimetable()!.id,
      dayOfWeek: dayValue,
      periodNumber: periodNum,
      startTime: periodData.start,
      endTime: periodData.end,
      classSubjectTeacherId: null,
      isBreak: false,
      roomId: null
    };

    this.roomService.getAvailable(dayValue, periodNum).subscribe({
      next: (data: Room[]) => this.availableRooms.set(data),
      error: () => this.availableRooms.set([])
    });
    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
  }

  saveSlot() {
    if (!this.newSlot.isBreak && !this.newSlot.classSubjectTeacherId) {
      // FIX 10: replace alert() with inline error banner
      this.showError('الرجاء اختيار المادة والمعلم قبل حفظ الحصة.');
      return;
    }
    this.timetableService.addSlot(this.newSlot).subscribe({
      next: () => {
        this.loadTimetable();
        this.closeModal();
        this.showSuccess('تم تسكين الحصة بنجاح');
      },
      // FIX 11: replace alert() with inline error banner
      error: () => this.showError('تعذر إضافة الحصة — تأكد من عدم وجود تعارض للمعلم أو القاعة.')
    });
  }

  getRemainingPeriods(assignment: ClassSubjectTeacher): number {
    const totalAssigned = assignment.weeklyPeriods;
    if (!this.activeTimetable() || !this.activeTimetable()!.slots) return totalAssigned;
    const used = this.activeTimetable()!.slots!.filter((s: any) => s.classSubjectTeacherId == assignment.id).length;
    return totalAssigned - used;
  }

  // ── Notification helpers ────────────────────────────────────────────────────

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

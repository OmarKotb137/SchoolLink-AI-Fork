import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { TimetableService } from '../../core/services/timetable.service';

// Mirrors TimetableSlotDto from the backend
export interface ClassScheduleSlot {
  id:                    number;
  timetableId:           number;
  dayOfWeek:             string;
  periodNumber:          number;
  startTime:             string;
  endTime:               string;
  isBreak:               boolean;
  classSubjectTeacherId: number | null;
  subjectName:           string | null;
  teacherName:           string | null;
  roomId:                number | null;
  roomName:              string | null;
}

// Mirrors TimetableDto from the backend
export interface ClassScheduleData {
  id:            number;
  classId:       number;
  className:     string;
  academicYearId: number;
  isActive:      boolean;
  slots:         ClassScheduleSlot[];
}

@Component({
  selector: 'app-class-schedule',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './class-schedule.html',
  styleUrl: './class-schedule.css',
})
export class ClassSchedule implements OnInit {
  sidebarOpen  = signal(false);

  private timetableService = inject(TimetableService);

  // FIX 1: بدل ما نخزّن الـ OperationResult كله نسحب الـ TimetableDto مباشرة من .data
  //        فـ getSlot() والـ template بيشتغلوا على الـ object الحقيقي مش الـ wrapper
  timetable    = signal<ClassScheduleData | null>(null);
  isLoading    = signal(true);
  errorMessage = signal<string | null>(null);
  hasLoaded    = signal(false);

  days = [
    { value: 'Sunday',    label: 'الأحد' },
    { value: 'Monday',    label: 'الإثنين' },
    { value: 'Tuesday',   label: 'الثلاثاء' },
    { value: 'Wednesday', label: 'الأربعاء' },
    { value: 'Thursday',  label: 'الخميس' }
  ];

  periods = [
    { num: 1, label: 'الحصة الأولى',  start: '08:00', end: '08:45' },
    { num: 2, label: 'الحصة الثانية', start: '08:45', end: '09:30' },
    { num: 3, label: 'الحصة الثالثة', start: '09:30', end: '10:15' },
    { num: 4, label: 'الحصة الرابعة', start: '10:30', end: '11:15' },
    { num: 5, label: 'الحصة الخامسة', start: '11:15', end: '12:00' },
    { num: 6, label: 'الحصة السادسة', start: '12:00', end: '12:45' },
    { num: 7, label: 'الحصة السابعة', start: '12:45', end: '13:30' }
  ];

  ngOnInit() {
    this.loadSchedule();
  }

  loadSchedule() {
    // FIX 2: reset الـ state عند كل محاولة تحميل (مهم لزرار إعادة المحاولة)
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.hasLoaded.set(false);

    this.timetableService.getMyStudentScheduleCurrentYear().subscribe({
      next: (response) => {
        // FIX 3 (CRITICAL): الـ API بيرجع OperationResult<TimetableDto>
        //   أي: { isSuccess: true, data: { id, classId, className, slots:[...] }, message: "..." }
        //   الكود القديم كان بيحط الـ response كله في scheduleData ثم بيشوف .slots
        //   لكن .slots على الـ OperationResult مش موجود — الـ TimetableDto بييجي في .data
        this.timetable.set(response?.data ?? null);
        this.hasLoaded.set(true);
        this.isLoading.set(false);
      },
      error: () => {
        this.timetable.set(null);
        this.hasLoaded.set(true);
        this.isLoading.set(false);
        this.errorMessage.set('تعذر تحميل الجدول الدراسي. يرجى المحاولة مرة أخرى.');
      }
    });
  }

  // FIX 4: بتشوف في timetable().slots مباشرة — مش scheduleData().slots
  //        والـ template بيستخدم "*ngIf="getSlot() as slot"
  //        فبتتنادى مرة واحدة بس للخلية بدل 4-5 مرات
  getSlot(dayValue: string, periodNum: number): ClassScheduleSlot | null {
    return this.timetable()?.slots?.find(
      s => s.dayOfWeek === dayValue && s.periodNumber === periodNum
    ) ?? null;
  }
}

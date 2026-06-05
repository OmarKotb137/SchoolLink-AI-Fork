import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { TimetableService } from '../../core/services/timetable.service';

// Mirrors TimetableSlotDto from the backend
export interface ParentScheduleSlot {
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

// Mirrors ChildScheduleDto from the backend
export interface ChildTimetable {
  id:             number;
  classId:        number;
  className:      string;
  academicYearId: number;
  isActive:       boolean;
  studentId:      number;
  studentName:    string;
  slots:          ParentScheduleSlot[];
}

@Component({
  selector: 'app-parent-schedule',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './parent-schedule.html',
  styleUrl: './parent-schedule.css',
})
export class ParentSchedule implements OnInit {
  sidebarOpen = signal(false);

  private timetableService = inject(TimetableService);

  // FIX 1: نخزّن الـ array من TimetableDto مباشرة بدل الـ OperationResult wrapper
  schedulesData         = signal<ChildTimetable[]>([]);
  selectedScheduleIndex = signal<number>(0);
  isLoading             = signal(true);
  errorMessage          = signal<string | null>(null);
  hasLoaded             = signal(false);

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

  // computed بدل getter عادي — بيستفيد من الـ signal reactivity
  currentSchedule = computed<ChildTimetable | null>(() => {
    const list = this.schedulesData();
    if (!list.length) return null;
    return list[this.selectedScheduleIndex()] ?? null;
  });

  ngOnInit() {
    this.loadSchedules();
  }

  loadSchedules() {
    // FIX 2: reset الـ state عند كل محاولة
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.hasLoaded.set(false);
    this.selectedScheduleIndex.set(0);

    this.timetableService.getMyChildSchedulesCurrentYear().subscribe({
      next: (response) => {
        // FIX 3 (CRITICAL): الـ API بيرجع OperationResult<IEnumerable<TimetableDto>>
        //   أي: { isSuccess: true, data: [...TimetableDto], message: "..." }
        //   الكود القديم كان بيحط الـ response كله في schedulesData
        //   فـ schedulesData() كانت بتبقى object مش array
        //   وده بيكسر *ngFor و .length وكل حاجة تانية
        const list: ChildTimetable[] = Array.isArray(response?.data) ? response.data : [];
        this.schedulesData.set(list);
        this.hasLoaded.set(true);
        this.isLoading.set(false);
      },
      error: () => {
        this.schedulesData.set([]);
        this.hasLoaded.set(true);
        this.isLoading.set(false);
        this.errorMessage.set('تعذر تحميل الجداول. يرجى المحاولة مرة أخرى.');
      }
    });
  }

  selectChild(index: number) {
    this.selectedScheduleIndex.set(index);
  }

  // FIX 4: بتشوف في currentSchedule().slots مباشرة
  //        والـ template بيستخدم "as slot" فبتتنادى مرة واحدة بس للخلية
  getSlot(dayValue: string, periodNum: number): ParentScheduleSlot | null {
    return this.currentSchedule()?.slots?.find(
      s => s.dayOfWeek === dayValue && s.periodNumber === periodNum
    ) ?? null;
  }
}

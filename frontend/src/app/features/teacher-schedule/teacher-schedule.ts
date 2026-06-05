import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { TimetableService } from '../../core/services/timetable.service';

// FIX 3: interface واضحة بدل any — بتعكس TeacherScheduleSlotDto من الـ backend
export interface TeacherScheduleSlot {
  id:                    number;
  timetableId:           number;
  classId:               number;
  className:             string;
  dayOfWeek:             string;
  periodNumber:          number;
  startTime:             string;
  endTime:               string;
  isBreak:               boolean;   // كان ناقص في الـ DTO وهنا كمان
  classSubjectTeacherId: number | null;
  subjectName:           string | null;
  roomName:              string | null;
}

@Component({
  selector: 'app-teacher-schedule',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './teacher-schedule.html',
  styleUrl: './teacher-schedule.css',
})
export class TeacherSchedule implements OnInit {
  sidebarOpen = signal(false);

  private timetableService = inject(TimetableService);

  // FIX 4: بدل ما نحتفظ بالـ OperationResult كاملاً (اللي فيه data + isSuccess + message)
  //        نخزّن الـ array مباشرة — فـ getSlot() والـ template مش محتاجين يعرفوا
  //        شكل الـ wrapper خالص.
  slots    = signal<TeacherScheduleSlot[]>([]);
  isLoading   = signal(true);
  errorMessage = signal<string | null>(null);
  // علشان نفرق بين "مفيش بيانات خالص" و "البيانات اتجابت بس فاضية"
  hasLoaded   = signal(false);

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
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.hasLoaded.set(false);

    this.timetableService.getMyScheduleCurrentYear().subscribe({
      next: (response) => {
        // FIX 5 (CRITICAL): الـ API بيرجع OperationResult<IEnumerable<TeacherScheduleSlotDto>>
        //   أي: { isSuccess: true, data: [...slots], message: "..." }
        //   الكود القديم كان بيحط الـ response كله في scheduleData ثم بيشوف .slots
        //   لكن مفيش .slots في الـ response — الـ array بييجي في .data
        this.slots.set(Array.isArray(response?.data) ? response.data : []);
        this.hasLoaded.set(true);
        this.isLoading.set(false);
      },
      error: () => {
        this.slots.set([]);
        this.hasLoaded.set(true);
        this.isLoading.set(false);
        this.errorMessage.set('تعذر تحميل الجدول الدراسي. يرجى المحاولة مرة أخرى.');
      }
    });
  }

  // FIX 6: getSlot بقت تشوف في الـ array مباشرة — مفيش .slots ولا .data في النص
  //        والـ template بيستخدم "*ngIf="getSlot() as slot" فبتتنادى مرة واحدة بس للخلية
  getSlot(dayValue: string, periodNum: number): TeacherScheduleSlot | null {
    return this.slots().find(
      s => s.dayOfWeek === dayValue && s.periodNumber === periodNum
    ) ?? null;
  }
}

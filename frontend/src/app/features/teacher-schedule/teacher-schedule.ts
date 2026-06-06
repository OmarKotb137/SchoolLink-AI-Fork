import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { TimetableService } from '../../core/services/timetable.service';
import { TeacherScheduleSlotDto } from '../../core/models/timetable.models';
import {
  buildSchedulePeriods,
  SchedulePeriodView,
} from '../../core/utils/schedule-periods';

@Component({
  selector: 'app-teacher-schedule',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './teacher-schedule.html',
  styleUrl: './teacher-schedule.css',
})
export class TeacherSchedule implements OnInit {
  sidebarOpen = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'المعلم';

  private timetableService = inject(TimetableService);

  // FIX 4: بدل ما نحتفظ بالـ OperationResult كاملاً (اللي فيه data + isSuccess + message)
  //        نخزّن الـ array مباشرة — فـ getSlot() والـ template مش محتاجين يعرفوا
  //        شكل الـ wrapper خالص.
  slots    = signal<TeacherScheduleSlotDto[]>([]);
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

  periods = computed<SchedulePeriodView[]>(() => buildSchedulePeriods(this.slots()));

  ngOnInit() {
    this.loadSchedule();
  }

  loadSchedule() {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.hasLoaded.set(false);

    this.timetableService.getMyScheduleCurrentYear().subscribe({
      next: (response: any) => {
        const data = response.isSuccess ? (response.data ?? []) : [];
        this.slots.set(Array.isArray(data) ? data : []);
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
  getSlot(dayValue: string, periodNum: number): TeacherScheduleSlotDto | null {
    return this.slots().find(
      s => s.dayOfWeek === dayValue && s.periodNumber === periodNum
    ) ?? null;
  }
}

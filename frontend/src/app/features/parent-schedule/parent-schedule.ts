import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { TimetableService } from '../../core/services/timetable.service';
import { ChildScheduleDto, TimetableSlotDto } from '../../core/models/timetable.models';
import {
  buildSchedulePeriods,
  SchedulePeriodView,
} from '../../core/utils/schedule-periods';

@Component({
  selector: 'app-parent-schedule',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './parent-schedule.html',
  styleUrl: './parent-schedule.css',
})
export class ParentSchedule implements OnInit {
  sidebarOpen = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'ولي الأمر';

  private timetableService = inject(TimetableService);

  // FIX 1: نخزّن الـ array من TimetableDto مباشرة بدل الـ OperationResult wrapper
  schedulesData         = signal<ChildScheduleDto[]>([]);
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

  // computed بدل getter عادي — بيستفيد من الـ signal reactivity
  currentSchedule = computed<ChildScheduleDto | null>(() => {
    const list = this.schedulesData();
    if (!list.length) return null;
    return list[this.selectedScheduleIndex()] ?? null;
  });
  periods = computed<SchedulePeriodView[]>(() => buildSchedulePeriods(this.currentSchedule()?.slots ?? []));

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
        // apiInterceptor بيفك OperationResult تلقائياً، فـ response هنا هو الـ array نفسه.
        const list: ChildScheduleDto[] = Array.isArray(response) ? response : [];
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
  getSlot(dayValue: string, periodNum: number): TimetableSlotDto | null {
    return this.currentSchedule()?.slots?.find(
      s => s.dayOfWeek === dayValue && s.periodNumber === periodNum
    ) ?? null;
  }
}

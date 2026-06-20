import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { TimetableService } from '../../core/services/timetable.service';
import { ChildScheduleDto, TimetableSlotDto } from '../../core/models/timetable.models';
import {
  buildSchedulePeriods,
  getCurrentPeriodNumber,
  SchedulePeriodView,
} from '../../core/utils/schedule-periods';

@Component({
  selector: 'app-parent-schedule',
  standalone: true,
  imports: [CommonModule, Sidebar],
  templateUrl: './parent-schedule.html',
  styleUrl: './parent-schedule.css',
})
export class ParentSchedule implements OnInit {
  sidebarOpen = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'ولي الأمر';

  private timetableService = inject(TimetableService);

  schedulesData         = signal<ChildScheduleDto[]>([]);
  selectedScheduleIndex = signal<number>(0);
  isLoading             = signal(true);
  errorMessage          = signal<string | null>(null);
  hasLoaded             = signal(false);
  mobileSelectedDay     = signal<string>(this.getDefaultDay());

  days = [
    { value: 'Sunday',    label: 'الأحد' },
    { value: 'Monday',    label: 'الإثنين' },
    { value: 'Tuesday',   label: 'الثلاثاء' },
    { value: 'Wednesday', label: 'الأربعاء' },
    { value: 'Thursday',  label: 'الخميس' }
  ];

  currentSchedule = computed<ChildScheduleDto | null>(() => {
    const list = this.schedulesData();
    if (!list.length) return null;
    return list[this.selectedScheduleIndex()] ?? null;
  });

  periods = computed<SchedulePeriodView[]>(() =>
    buildSchedulePeriods(this.currentSchedule()?.slots ?? [])
  );

  /** رقم الحصة الجارية الآن (للهايلايت) — null لو خارج وقت الدراسة */
  currentPeriodNumber = computed<number | null>(() =>
    getCurrentPeriodNumber(this.periods())
  );

  ngOnInit() {
    this.loadSchedules();
  }

  loadSchedules() {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.hasLoaded.set(false);
    this.selectedScheduleIndex.set(0);

    this.timetableService.getMyChildSchedulesCurrentYear().subscribe({
      next: (response: any) => {
        const data = response.isSuccess ? (response.data ?? []) : [];
        const list: ChildScheduleDto[] = Array.isArray(data) ? data : [];
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

  /** تغيير اليوم المعروض في عرض الموبايل (أجندة يوم بيوم) */
  selectMobileDay(dayValue: string) {
    this.mobileSelectedDay.set(dayValue);
  }

  /** اليوم الحالي كقيمة افتراضية لعرض الموبايل، أو أول يوم دراسي لو النهاردة عطلة */
  private getDefaultDay(): string {
    const jsDay = new Date().getDay();
    const map: Record<number, string> = {
      0: 'Sunday', 1: 'Monday', 2: 'Tuesday', 3: 'Wednesday', 4: 'Thursday'
    };
    return map[jsDay] ?? 'Sunday';
  }

  getSlot(dayValue: string, periodNum: number): TimetableSlotDto | null {
    return this.currentSchedule()?.slots?.find(
      s => s.dayOfWeek === dayValue && s.periodNumber === periodNum
    ) ?? null;
  }

  /** Returns true if dayValue matches today — used for row highlight */
  isToday(dayValue: string): boolean {
    const jsDay = new Date().getDay();
    const map: Record<string, number> = {
      Sunday: 0, Monday: 1, Tuesday: 2,
      Wednesday: 3, Thursday: 4, Friday: 5, Saturday: 6
    };
    return map[dayValue] === jsDay;
  }

  /** Returns 0-6 color index based on subject name hash — purely visual */
  getSubjectColor(name?: string | null): number {
    if (!name) return 0;
    let h = 0;
    for (let i = 0; i < name.length; i++) {
      h = (h * 31 + name.charCodeAt(i)) & 0xffff;
    }
    return h % 7;
  }

  /** Returns avatar initials (up to 2 chars) from a name */
  getInitials(name?: string | null): string {
    if (!name) return '؟';
    const parts = name.trim().split(/\s+/);
    return parts.length >= 2
      ? parts[0][0] + parts[1][0]
      : parts[0].slice(0, 2);
  }
}

import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule }                       from '@angular/common';
import { Sidebar }                            from '../../layouts/sidebar/sidebar';
import { Topbar }                             from '../../layouts/topbar/topbar';
import { TimetableService }                   from '../../core/services/timetable.service';
import { TimetableDto, TimetableSlotDto }     from '../../core/models/timetable.models';
import {
  buildSchedulePeriods,
  getCurrentPeriodNumber,
  SchedulePeriodView,
} from '../../core/utils/schedule-periods';

@Component({
  selector:    'app-class-schedule',
  standalone:  true,
  imports:     [CommonModule, Sidebar, Topbar],
  templateUrl: './class-schedule.html',
  styleUrl:    './class-schedule.css',
})
export class ClassSchedule implements OnInit {
  sidebarOpen  = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'الطالب';

  private timetableService = inject(TimetableService);

  timetable    = signal<TimetableDto | null>(null);
  isLoading    = signal(true);
  errorMessage = signal<string | null>(null);
  hasLoaded    = signal(false);

  /* ── helpers ─────────────────────────────────────────────────── */

  /** اليوم الحالي بالإنجليزي */
  readonly todayValue = (() => {
    const d = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
    return d[new Date().getDay()];
  })();

  days = [
    { value: 'Sunday',    label: 'الأحد' },
    { value: 'Monday',    label: 'الإثنين' },
    { value: 'Tuesday',   label: 'الثلاثاء' },
    { value: 'Wednesday', label: 'الأربعاء' },
    { value: 'Thursday',  label: 'الخميس' },
  ];

  periods = computed<SchedulePeriodView[]>(() => buildSchedulePeriods(this.timetable()?.slots ?? []));

  /* ── lifecycle ─────────────────────────────────────────────────── */

  ngOnInit() { this.loadSchedule(); }

  loadSchedule() {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.hasLoaded.set(false);

    this.timetableService.getMyStudentScheduleCurrentYear().subscribe({
      next: (response: any) => {
        if (response.isSuccess) this.timetable.set(response.data ?? null);
        this.hasLoaded.set(true);
        this.isLoading.set(false);
      },
      error: () => {
        this.timetable.set(null);
        this.hasLoaded.set(true);
        this.isLoading.set(false);
        this.errorMessage.set('تعذر تحميل الجدول الدراسي. يرجى المحاولة مرة أخرى.');
      },
    });
  }

  /* ── grid helpers ─────────────────────────────────────────────── */

  getSlot(dayValue: string, periodNum: number): TimetableSlotDto | null {
    return this.timetable()?.slots?.find(
      s => s.dayOfWeek === dayValue && s.periodNumber === periodNum
    ) ?? null;
  }

  isToday(dayValue: string): boolean {
    return dayValue === this.todayValue;
  }

  /** رقم الحصة الحالية بناءً على الوقت، أو null لو مش في وقت حصة */
  getCurrentPeriodNum(): number | null {
    return getCurrentPeriodNumber(this.periods());
  }

  /* ── subject color ────────────────────────────────────────────── */

  private readonly palette = [
    'sch-subj-blue',
    'sch-subj-cyan',
    'sch-subj-green',
    'sch-subj-orange',
    'sch-subj-purple',
  ];

  getSubjectColor(name: string | null): string {
    if (!name) return 'sch-subj-gray';
    let h = 0;
    for (let i = 0; i < name.length; i++) h = name.charCodeAt(i) + ((h << 5) - h);
    return this.palette[Math.abs(h) % this.palette.length];
  }

  /* ── stats ────────────────────────────────────────────────────── */

  get lessonCount(): number {
    return this.timetable()?.slots?.filter(s => !s.isBreak).length ?? 0;
  }

  get uniqueSubjects(): Array<{ name: string; color: string }> {
    const seen = new Map<string, string>();
    this.timetable()?.slots?.forEach(s => {
      if (!s.isBreak && s.subjectName && !seen.has(s.subjectName))
        seen.set(s.subjectName, this.getSubjectColor(s.subjectName));
    });
    return [...seen.entries()].map(([name, color]) => ({ name, color }));
  }

  /** اسم اليوم الحالي بالعربي */
  getTodayLabel(): string {
    return this.days.find(d => d.value === this.todayValue)?.label ?? '';
  }
}

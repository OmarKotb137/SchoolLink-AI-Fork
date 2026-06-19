import { Component, OnInit, OnDestroy, computed, inject, signal } from '@angular/core';
import { CommonModule }                       from '@angular/common';
import { Sidebar }                            from '../../layouts/sidebar/sidebar';
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
  imports:     [CommonModule, Sidebar],
  templateUrl: './class-schedule.html',
  styleUrl:    './class-schedule.css',
})
export class ClassSchedule implements OnInit, OnDestroy {
  sidebarOpen  = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'الطالب';

  private timetableService = inject(TimetableService);

  timetable    = signal<TimetableDto | null>(null);
  isLoading    = signal(true);
  errorMessage = signal<string | null>(null);
  hasLoaded    = signal(false);

  /* ── live clock tick ─────────────────────────────────────────── */
  /** نبضة بسيطة كل 30 ثانية فقط لتحديث "الحصة الحالية" تلقائيًا من غير ما المستخدم يعمل أي حركة */
  nowTick = signal(Date.now());
  private liveTimer?: ReturnType<typeof setInterval>;

  /* ── mobile agenda state (UI-only, لا يلمس البيانات) ─────────── */
  selectedDay = signal<string>('');

  selectDay(value: string) {
    this.selectedDay.set(value);
  }

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

  ngOnInit() {
    // الافتراضي على الموبايل: يفتح على اليوم الحالي لو ضمن أيام الدراسة، وإلا أول يوم
    this.selectedDay.set(this.days.some(d => d.value === this.todayValue) ? this.todayValue : this.days[0].value);
    this.loadSchedule();
    this.liveTimer = setInterval(() => this.nowTick.set(Date.now()), 30000);
  }

  ngOnDestroy() {
    if (this.liveTimer) clearInterval(this.liveTimer);
  }

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
    this.nowTick();
    return getCurrentPeriodNumber(this.periods());
  }

  isCurrentCell(dayValue: string, periodNum: number): boolean {
    return this.isToday(dayValue) && this.getCurrentPeriodNum() === periodNum;
  }

  /** الحصة الجارية دلوقتي بالنسبة لليوم الحالي فقط (لعرضها في الهيدر) */
  getCurrentLessonSlot(): TimetableSlotDto | null {
    const periodNum = this.getCurrentPeriodNum();
    if (periodNum === null) return null;
    return this.getSlot(this.todayValue, periodNum);
  }

  /* ── subject color & icon ────────────────────────────────────── */

  private readonly palette = [
    'cs-subj-blue',
    'cs-subj-cyan',
    'cs-subj-green',
    'cs-subj-orange',
    'cs-subj-purple',
  ];

  private readonly icons = [
    'calculate',
    'language',
    'biotech',
    'history_edu',
    'palette',
  ];

  private hashName(name: string): number {
    let h = 0;
    for (let i = 0; i < name.length; i++) h = name.charCodeAt(i) + ((h << 5) - h);
    return Math.abs(h);
  }

  getSubjectColor(name: string | null): string {
    if (!name) return 'cs-subj-gray';
    return this.palette[this.hashName(name) % this.palette.length];
  }

  getSubjectIcon(name: string | null): string {
    if (!name) return 'menu_book';
    return this.icons[this.hashName(name) % this.icons.length];
  }

  /* ── stats ────────────────────────────────────────────────────── */

  get lessonCount(): number {
    return this.timetable()?.slots?.filter(s => !s.isBreak).length ?? 0;
  }

  get uniqueSubjects(): Array<{ name: string; color: string; icon: string }> {
    const seen = new Map<string, { color: string; icon: string }>();
    this.timetable()?.slots?.forEach(s => {
      if (!s.isBreak && s.subjectName && !seen.has(s.subjectName))
        seen.set(s.subjectName, { color: this.getSubjectColor(s.subjectName), icon: this.getSubjectIcon(s.subjectName) });
    });
    return [...seen.entries()].map(([name, v]) => ({ name, color: v.color, icon: v.icon }));
  }

  /** اسم اليوم الحالي بالعربي */
  getTodayLabel(): string {
    return this.days.find(d => d.value === this.todayValue)?.label ?? '';
  }

  /** هل اليوم الحالي ضمن أيام الدراسة المعروضة؟ */
  get isTodaySchoolDay(): boolean {
    return this.days.some(d => d.value === this.todayValue);
  }
}

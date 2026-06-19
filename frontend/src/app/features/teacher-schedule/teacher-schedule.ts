import { Component, OnInit, OnDestroy, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { TimetableService } from '../../core/services/timetable.service';
import { TeacherScheduleSlotDto } from '../../core/models/timetable.models';
import {
  buildSchedulePeriods,
  getCurrentPeriodNumber,
  SchedulePeriodView,
} from '../../core/utils/schedule-periods';

@Component({
  selector: 'app-teacher-schedule',
  standalone: true,
  imports: [CommonModule, Sidebar],
  templateUrl: './teacher-schedule.html',
  styleUrl: './teacher-schedule.css',
})
export class TeacherSchedule implements OnInit, OnDestroy {
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

  /* ── live clock tick: تحديث "الحصة الحالية" تلقائيًا كل 30 ثانية ── */
  nowTick = signal(Date.now());
  private liveTimer?: ReturnType<typeof setInterval>;

  /* ── mobile agenda state (UI-only) ───────────────────────────── */
  selectedDay = signal<string>('');

  selectDay(value: string) {
    this.selectedDay.set(value);
  }

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
    { value: 'Thursday',  label: 'الخميس' }
  ];

  periods = computed<SchedulePeriodView[]>(() => buildSchedulePeriods(this.slots()));

  ngOnInit() {
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

  /* ── today / live period (إضافة جديدة فقط — مفيش لمس لمنطق الداتا) ── */

  isToday(dayValue: string): boolean {
    return dayValue === this.todayValue;
  }

  get isTodaySchoolDay(): boolean {
    return this.days.some(d => d.value === this.todayValue);
  }

  getTodayLabel(): string {
    return this.days.find(d => d.value === this.todayValue)?.label ?? '';
  }

  getCurrentPeriodNum(): number | null {
    this.nowTick();
    return getCurrentPeriodNumber(this.periods());
  }

  isCurrentCell(dayValue: string, periodNum: number): boolean {
    return this.isToday(dayValue) && this.getCurrentPeriodNum() === periodNum;
  }

  getCurrentLessonSlot(): TeacherScheduleSlotDto | null {
    const periodNum = this.getCurrentPeriodNum();
    if (periodNum === null) return null;
    return this.getSlot(this.todayValue, periodNum);
  }

  /* ── subject color & icon (نفس فكرة صفحة الطالب) ─────────────── */

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
    return this.slots().filter(s => !s.isBreak).length;
  }

  get uniqueSubjects(): Array<{ name: string; color: string; icon: string }> {
    const seen = new Map<string, { color: string; icon: string }>();
    this.slots().forEach(s => {
      if (!s.isBreak && s.subjectName && !seen.has(s.subjectName))
        seen.set(s.subjectName, { color: this.getSubjectColor(s.subjectName), icon: this.getSubjectIcon(s.subjectName) });
    });
    return [...seen.entries()].map(([name, v]) => ({ name, color: v.color, icon: v.icon }));
  }

  get uniqueClassesCount(): number {
    const seen = new Set<string>();
    this.slots().forEach(s => { if (!s.isBreak && s.className) seen.add(s.className); });
    return seen.size;
  }
}

import { Component, signal, computed } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

interface ScheduleCell {
  subject: string;
  class: string;
  room: string;
  teacher: string;
  color: 'blue' | 'cyan' | 'green' | 'orange' | 'purple' | 'none';
}

interface Period {
  label: string;
  time: string;
  start: string;
  end: string;
}

interface DayRow {
  dayAr: string;
  dayEn: string;
  cells: ScheduleCell[];
}

@Component({
  selector: 'app-class-schedule',
  imports: [Sidebar, Topbar],
  templateUrl: './class-schedule.html',
  styleUrl: './class-schedule.css',
})
export class ClassSchedule {
  sidebarOpen = signal(false);

  periods: Period[] = [
    { label: 'الحصة 1', time: '8:00 - 9:00', start: '08:00', end: '09:00' },
    { label: 'الحصة 2', time: '9:00 - 10:00', start: '09:00', end: '10:00' },
    { label: 'استراحة', time: '10:00 - 10:30', start: '10:00', end: '10:30', },
    { label: 'الحصة 3', time: '10:30 - 11:30', start: '10:30', end: '11:30' },
    { label: 'الحصة 4', time: '11:30 - 12:30', start: '11:30', end: '12:30' },
    { label: 'الحصة 5', time: '12:30 - 1:30', start: '12:30', end: '13:30' },
  ];

  days: DayRow[] = [
    {
      dayAr: 'السبت', dayEn: 'Saturday',
      cells: [
        { subject: 'الرياضيات', class: 'ثالث إعدادي أ', room: 'معمل الرياضيات', teacher: 'أ. أحمد سالم', color: 'blue' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: 'العلوم', class: 'ثالث إعدادي أ', room: 'معمل العلوم', teacher: 'أ. فاطمة حسن', color: 'green' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
      ],
    },
    {
      dayAr: 'الأحد', dayEn: 'Sunday',
      cells: [
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: 'الرياضيات', class: 'ثالث إعدادي أ', room: 'معمل الرياضيات', teacher: 'أ. أحمد سالم', color: 'blue' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: 'اللغة العربية', class: 'ثالث إعدادي أ', room: 'القاعة 2', teacher: 'أ. محمد علي', color: 'orange' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: 'الرياضيات', class: 'ثالث إعدادي أ', room: 'معمل الرياضيات', teacher: 'أ. أحمد سالم', color: 'blue' },
      ],
    },
    {
      dayAr: 'الاثنين', dayEn: 'Monday',
      cells: [
        { subject: 'اللغة العربية', class: 'ثالث إعدادي ب', room: 'القاعة 4', teacher: 'أ. محمد علي', color: 'orange' },
        { subject: 'الرياضيات', class: 'ثالث إعدادي أ', room: 'معمل الرياضيات', teacher: 'أ. أحمد سالم', color: 'blue' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: 'اللغة الإنجليزية', class: 'ثالث إعدادي ب', room: 'القاعة 4', teacher: 'أ. سارة أحمد', color: 'purple' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
      ],
    },
    {
      dayAr: 'الثلاثاء', dayEn: 'Tuesday',
      cells: [
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: 'الرياضيات', class: 'ثالث إعدادي أ', room: 'معمل الرياضيات', teacher: 'أ. أحمد سالم', color: 'blue' },
        { subject: 'العلوم', class: 'ثالث إعدادي أ', room: 'معمل العلوم', teacher: 'أ. فاطمة حسن', color: 'green' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
      ],
    },
    {
      dayAr: 'الأربعاء', dayEn: 'Wednesday',
      cells: [
        { subject: 'العلوم', class: 'ثالث إعدادي أ', room: 'معمل العلوم', teacher: 'أ. فاطمة حسن', color: 'green' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: 'الرياضيات', class: 'ثالث إعدادي ب', room: 'معمل الرياضيات', teacher: 'أ. أحمد سالم', color: 'blue' },
        { subject: 'اللغة الإنجليزية', class: 'ثالث إعدادي أ', room: 'القاعة 2', teacher: 'أ. سارة أحمد', color: 'purple' },
      ],
    },
    {
      dayAr: 'الخميس', dayEn: 'Thursday',
      cells: [
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: 'اللغة العربية', class: 'ثالث إعدادي أ', room: 'القاعة 2', teacher: 'أ. محمد علي', color: 'orange' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: '', class: '', room: '', teacher: '', color: 'none' },
        { subject: 'اللغة العربية', class: 'ثالث إعدادي ب', room: 'القاعة 4', teacher: 'أ. محمد علي', color: 'orange' },
      ],
    },
  ];

  dayNames = ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

  currentDayIndex = signal(new Date().getDay());
  currentTime = signal(this.getTimeStr());

  currentDayAr = computed(() => {
    const idx = this.currentDayIndex();
    return this.dayNames[idx];
  });

  currentPeriodIndex = computed(() => {
    const now = this.currentTime();
    return this.periods.findIndex(p => now >= p.start && now < p.end);
  });

  private getTimeStr(): string {
    const d = new Date();
    return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
  }

  getColorClass(color: string): string {
    const map: Record<string, string> = {
      blue: 'class-blue',
      cyan: 'class-cyan',
      green: 'class-green',
      orange: 'class-orange',
      purple: 'class-purple',
    };
    return map[color] || '';
  }

  getPeriodBgColor(pi: number): string {
    if (pi === 2) return ''; // break
    const cp = this.currentPeriodIndex();
    if (cp >= 0 && pi === cp) return 'bg-primary/10';
    return '';
  }

  isCurrentDay(di: number): boolean {
    return di === this.currentDayIndex();
  }

  isCurrentPeriod(pi: number): boolean {
    const cp = this.currentPeriodIndex();
    if (cp < 0) return false;
    if (pi === 2) return false; // break
    return pi === cp;
  }

  isCurrentCell(di: number, pi: number): boolean {
    return this.isCurrentDay(di) && this.isCurrentPeriod(pi);
  }

  getStudent() {
    return { name: 'عمر محمود', class: 'ثالث إعدادي أ' };
  }

  str(i: number): string { return String(i); }
}

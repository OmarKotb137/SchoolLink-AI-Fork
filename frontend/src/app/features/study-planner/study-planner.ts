import { Component, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-study-planner',
  imports: [Sidebar, Topbar, FormsModule],
  templateUrl: './study-planner.html',
  styleUrl: './study-planner.css',
})
export class StudyPlanner {
  sidebarOpen = signal(false);
  timePopupVisible = signal(false);
  activeTimeSpan: HTMLElement | null = null;
  restDayIndex = signal<number | null>(6);

  hours = Array.from({ length: 12 }, (_, i) => {
    const v = i + 1;
    return v < 10 ? '0' + v : '' + v;
  });
  minutes = Array.from({ length: 12 }, (_, i) => {
    const v = i * 5;
    return v < 10 ? '0' + v : '' + v;
  });

  editStartHour = '08';
  editStartMin = '00';
  editStartPeriod = 'ص';
  editEndHour = '12';
  editEndMin = '00';
  editEndPeriod = 'م';

  periods = [
    { label: 'الصباح', icon: 'wb_sunny', start: '08:00 ص', end: '12:00 م', color: '#003d9b' },
    { label: 'الظهر', icon: 'light_mode', start: '12:00 م', end: '04:00 م', color: '#432f9c' },
    { label: 'المساء', icon: 'dark_mode', start: '04:00 م', end: '08:00 م', color: '#00687b' },
    { label: 'الليل', icon: 'nights_stay', start: '08:00 م', end: '10:00 م', color: '#432f9c' },
  ];

  days = ['السبت', 'الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة'];

  /** Days before the rest day (for rendering regular columns) */
  daysBeforeRest = computed(() => {
    const ri = this.restDayIndex();
    if (ri === null) return this.days;
    return this.days.slice(0, ri);
  });

  /** Days after the rest day (for rendering regular columns) */
  daysAfterRest = computed(() => {
    const ri = this.restDayIndex();
    if (ri === null) return [];
    return this.days.slice(ri + 1);
  });

  sessions: Record<string, { subject: string; topic: string; duration: string; color: string; colorBorder: string; weak?: boolean } | null> = {
    '0-1': { subject: 'رياضيات', topic: 'المعادلات', duration: '45د', color: '#003d9b', colorBorder: '#003d9b' },
    '0-2': { subject: 'علوم', topic: 'الكهرباء', duration: '60د', color: '#00687b', colorBorder: '#00687b' },
    '1-1': { subject: 'إنجليزي', topic: 'Vocab Unit 5', duration: '30د', color: '#432f9c', colorBorder: '#432f9c', weak: true },
    '1-2': { subject: 'عربي', topic: 'القراءة', duration: '20د', color: '#10b981', colorBorder: '#10b981' },
    '2-1': { subject: 'إنجليزي', topic: 'Grammar', duration: '30د', color: '#432f9c', colorBorder: '#432f9c' },
  };

  completed = signal<Set<string>>(new Set());

  totalSessions = computed(() => Object.keys(this.sessions).length);
  completedCount = computed(() => this.completed().size);
  completionPct = computed(() => {
    const t = this.totalSessions();
    return t ? Math.round(this.completedCount() / t * 100) : 0;
  });

  toggleComplete(key: string, event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    this.completed.update(s => {
      const next = new Set(s);
      if (checked) next.add(key);
      else next.delete(key);
      return next;
    });
  }

  getSession(pi: number, di: number) {
    const key = pi + '-' + di;
    return { key, session: this.sessions[key] ?? null };
  }

  openTimePopup(el: HTMLElement) {
    this.activeTimeSpan = el;
    const txt = el.textContent?.trim().replace(/\s+/g, ' ') ?? '';
    const parts = txt.split('-');
    if (parts.length === 2) {
      const start = parts[0].trim().split(' ');
      const end = parts[1].trim().split(' ');
      if (start.length === 2) {
        const st = start[0].split(':');
        if (st.length === 2) { this.editStartHour = st[0]; this.editStartMin = st[1]; }
        this.editStartPeriod = start[1];
      }
      if (end.length === 2) {
        const et = end[0].split(':');
        if (et.length === 2) { this.editEndHour = et[0]; this.editEndMin = et[1]; }
        this.editEndPeriod = end[1];
      }
    }
    this.timePopupVisible.set(true);
  }

  closeTimePopup() {
    this.timePopupVisible.set(false);
    this.activeTimeSpan = null;
  }

  saveTimePopup() {
    if (this.activeTimeSpan) {
      this.activeTimeSpan.textContent =
        this.editStartHour + ':' + this.editStartMin + ' ' + this.editStartPeriod + ' - ' +
        this.editEndHour + ':' + this.editEndMin + ' ' + this.editEndPeriod;
    }
    this.closeTimePopup();
  }
}

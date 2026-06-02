import { Component, signal, computed } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-teacher-schedule',
  imports: [Sidebar, Topbar],
  templateUrl: './teacher-schedule.html',
  styleUrl: './teacher-schedule.css'
})
export class TeacherSchedule {
  sidebarOpen = signal(false);
  teacherName = 'أ. أحمد سالم';
  subject = 'الرياضيات';
  classes = 5;

  days = ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس'];

  periods = [
    { label: 'الحصة 1', start: '07:30', end: '08:30' },
    { label: 'الحصة 2', start: '08:30', end: '09:30' },
    { label: 'الحصة 3', start: '09:30', end: '10:30' },
    { label: 'استراحة', start: '10:30', end: '11:00', isBreak: true },
    { label: 'الحصة 4', start: '11:00', end: '12:00' },
    { label: 'الحصة 5', start: '12:00', end: '13:00' },
    { label: 'الحصة 6', start: '13:00', end: '14:00' }
  ];

  schedule: Record<string, { name: string; loc: string; type: number }> = {
    '0-0': { name: '٣-أ', loc: 'قاعة ١٠١', type: 0 },
    '0-2': { name: '١-ج', loc: 'مختبر ٣', type: 1 },
    '0-4': { name: '٢-ب', loc: 'قاعة ٢٠٤', type: 2 },
    '1-1': { name: '٣-أ', loc: 'قاعة ١٠١', type: 0 },
    '1-3': { name: '١-ج', loc: 'مختبر ٣', type: 1 },
    '2-0': { name: '٢-ب', loc: 'قاعة ٢٠٤', type: 2 },
    '2-2': { name: '٣-أ', loc: 'قاعة ١٠١', type: 0 },
    '2-4': { name: '١-ج', loc: 'مختبر ٣', type: 1 },
    '4-1': { name: '٢-ب', loc: 'قاعة ٢٠٤', type: 2 },
    '4-3': { name: '٣-أ', loc: 'قاعة ١٠١', type: 0 },
    '5-0': { name: '١-ج', loc: 'مختبر ٣', type: 1 },
    '5-2': { name: '٢-ب', loc: 'قاعة ٢٠٤', type: 2 },
    '5-4': { name: '٣-أ', loc: 'قاعة ١٠١', type: 0 }
  };

  classTypes = ['busy', 'busy-cyan', 'busy-green', 'busy-orange', 'busy-purple'];

  today = new Date().toLocaleDateString('ar-EG', { weekday:'long', year:'numeric', month:'long', day:'numeric' });

  stats = computed(() => {
    let busy = 0, free = 0, totalHours = 0;
    for (const period of this.periods) {
      if (period.isBreak) continue;
      for (let di = 0; di < this.days.length; di++) {
        const cell = this.schedule[this.periods.indexOf(period) + '-' + di];
        if (cell) {
          busy++;
          const [sh, sm] = period.start.split(':').map(Number);
          const [eh, em] = period.end.split(':').map(Number);
          totalHours += (eh * 60 + em - sh * 60 - sm) / 60;
        } else {
          free++;
        }
      }
    }
    return { busy, free, totalHours };
  });

  get busyCount() { return this.stats().busy; }
  get freeCount() { return this.stats().free; }
  get totalHours() { return this.stats().totalHours; }

  getCell(pi: number, di: number) {
    return this.schedule[pi + '-' + di] ?? null;
  }
}
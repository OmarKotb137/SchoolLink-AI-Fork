import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-admin-schedule',
  imports: [Sidebar, Topbar],
  templateUrl: './admin-schedule.html',
  styleUrl: './admin-schedule.css'
})
export class AdminSchedule {
  sidebarOpen = signal(false);
  daySettingsOpen = false;
  editTimesMode = false;

  days = ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس'];

  periods = [
    { label: 'الحصة 1', start: '08:00', end: '09:00' },
    { label: 'الحصة 2', start: '09:00', end: '10:00' },
    { label: 'استراحة', start: '10:00', end: '10:30', isBreak: true },
    { label: 'الحصة 3', start: '10:30', end: '11:30' },
    { label: 'الحصة 4', start: '11:30', end: '12:30' },
    { label: 'الحصة 5', start: '12:30', end: '13:30' },
  ];

  cells: Record<string, string | null> = {
    '0-0': 'ثالث إعدادي أ\nمعمل الرياضيات\nأ. أحمد سالم',
    '0-2': 'ثالث إعدادي ب\nالقاعة 4\nأ. محمد علي',
    '0-3': 'ثالث إعدادي أ\nمعمل الرياضيات\nأ. أحمد سالم',
    '1-1': 'ثالث إعدادي أ\nمعمل الرياضيات\nأ. أحمد سالم',
    '1-3': 'ثالث إعدادي ب\nالقاعة 4\nأ. محمد علي',
    '1-4': 'ثالث إعدادي أ\nمعمل الرياضيات\nأ. أحمد سالم',
    '3-0': 'ثالث إعدادي ب\nالقاعة 4\nأ. محمد علي',
    '3-2': 'ثالث إعدادي أ\nمعمل الرياضيات\nأ. أحمد سالم',
    '3-4': 'ثالث إعدادي ب\nالقاعة 4\nأ. محمد علي',
    '4-1': 'ثالث إعدادي ب\nالقاعة 4\nأ. محمد علي',
    '4-3': 'ثالث إعدادي أ\nمعمل الرياضيات\nأ. أحمد سالم',
    '5-0': 'ثالث إعدادي أ\nمعمل الرياضيات\nأ. أحمد سالم',
    '5-2': 'ثالث إعدادي ب\nالقاعة 4\nأ. محمد علي',
    '5-4': 'ثالث إعدادي أ\nمعمل الرياضيات\nأ. أحمد سالم',
  };

  toggleDaySettings() {
    this.daySettingsOpen = !this.daySettingsOpen;
  }

  toggleEditTimes() {
    this.editTimesMode = !this.editTimesMode;
  }

  getCell(pi: number, di: number) {
    return this.cells[pi + '-' + di] ?? null;
  }

  addClass(pi: number, di: number) {
    const confirmed = confirm('هل تريد إضافة حصة جديدة في هذا الوقت؟');
    if (confirmed) {
      this.cells[pi + '-' + di] = 'فصل جديد\nقيد التحديد\nأ. غير محدد';
    }
  }

  deleteClass(pi: number, di: number) {
    const confirmed = confirm('هل أنت متأكد من حذف هذه الحصة؟');
    if (confirmed) {
      delete this.cells[pi + '-' + di];
    }
  }
}
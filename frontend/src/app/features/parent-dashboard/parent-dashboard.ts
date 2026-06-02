import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-parent-dashboard',
  imports: [Sidebar, Topbar],
  templateUrl: './parent-dashboard.html',
  styleUrl: './parent-dashboard.css'
})
export class ParentDashboard {
  sidebarOpen = signal(false);
  children = [
    { name: 'محمد أحمد', grade: 'الصف الثالث الثانوي', class: '3/1', performance: 88, grades: { last: '95', total: '88%' }, absences: 2 },
    { name: 'فاطمة أحمد', grade: 'الصف الأول الثانوي', class: '1/2', performance: 92, grades: { last: '98', total: '92%' }, absences: 0 },
  ];
  activities = [
    'حصل محمد على درجة 95 في اختبار الرياضيات',
    'تم تسليم واجب الكيمياء لفاطمة',
    'تحديث درجات السلوك لمحمد',
    'تسجيل غياب يوم الأحد - محمد'
  ];
}

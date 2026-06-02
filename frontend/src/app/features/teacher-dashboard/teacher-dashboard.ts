import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-teacher-dashboard',
  imports: [Sidebar, Topbar],
  templateUrl: './teacher-dashboard.html',
  styleUrl: './teacher-dashboard.css'
})
export class TeacherDashboard {
  sidebarOpen = signal(false);
  classes = [
    { name: 'الصف 3/1 - رياضيات', count: 35, color: 'bg-primary' },
    { name: 'الصف 3/2 - فيزياء', count: 32, color: 'bg-secondary' },
    { name: 'الصف 3/3 - كيمياء', count: 30, color: 'bg-amber-600' },
  ];
  tasks = ['تصحيح واجبات الرياضيات', 'تحضير درس الفيزياء', 'اجتماع مع ولي أمر الطالب علي'];
}

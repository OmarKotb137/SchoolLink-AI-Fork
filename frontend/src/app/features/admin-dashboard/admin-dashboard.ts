import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-admin-dashboard',
  imports: [Sidebar, Topbar],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css'
})
export class AdminDashboard {
  sidebarOpen = signal(false);
  activities = ['تسجيل طالب جديد - أحمد محمد', 'إضافة فصل جديد - 3/1', 'تحديث بيانات معلم', 'رفع تقرير أكاديمي جديد'];
  users = [
    { name: 'أحمد محمد', role: 'طالب', email: 'ahmed@school.com', status: 'نشط' },
    { name: 'سارة علي', role: 'معلم', email: 'sara@school.com', status: 'نشط' },
    { name: 'محمد عمر', role: 'طالب', email: 'mohamed@school.com', status: 'نشط' },
  ];
}

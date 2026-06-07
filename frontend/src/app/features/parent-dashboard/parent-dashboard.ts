import { Component, signal, OnInit, inject } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ParentDashboardService, ParentChild } from './parent-dashboard.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-parent-dashboard',
  imports: [Sidebar, Topbar],
  templateUrl: './parent-dashboard.html',
  styleUrl: './parent-dashboard.css'
})
export class ParentDashboard implements OnInit {
  private dashboardService = inject(ParentDashboardService);
  private authService = inject(AuthService);

  sidebarOpen = signal(false);
  userName = this.authService.user()?.fullName ?? 'وليّ أمر';
  children: ParentChild[] = [
    { name: 'محمد أحمد', grade: 'الصف الثالث الثانوي', class: '3/1', performance: 88, grades: { last: '95', total: '88%' }, absences: 2 },
    { name: 'فاطمة أحمد', grade: 'الصف الأول الثانوي', class: '1/2', performance: 92, grades: { last: '98', total: '92%' }, absences: 0 },
  ];
  activities: string[] = [
    'حصل محمد على درجة 95 في اختبار الرياضيات',
    'تم تسليم واجب الكيمياء لفاطمة',
    'تحديث درجات السلوك لمحمد',
    'تسجيل غياب يوم الأحد - محمد'
  ];

  ngOnInit() {
    const user = this.authService.user();
    if (user) {
      this.dashboardService.get().subscribe({
        next: (res) => {
          if (res.children.length > 0) {
            this.children = res.children;
          }
          if (res.recentActivities.length > 0) {
            this.activities = res.recentActivities;
          }
        }
      });
    }
  }
}

import { Component, OnInit, signal, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { AuthService } from '../../core/services/auth.service';
import { TeacherDashboardService } from '../../core/services/teacher-dashboard.service';

@Component({
  selector: 'app-teacher-dashboard',
  imports: [Sidebar, RouterModule],
  templateUrl: './teacher-dashboard.html',
  styleUrl: './teacher-dashboard.css'
})
export class TeacherDashboard implements OnInit {
  private authService = inject(AuthService);
  private dashboardService = inject(TeacherDashboardService);

  sidebarOpen = signal(false);
  userName = signal('المعلم');
  todayClassesCount = signal(0);
  totalStudentsCount = signal(0);
  pendingSubmissionsCount = signal(0);
  isLoading = signal(true);

  classes = signal<any[]>([]);
  tasks = signal<string[]>([]);

  ngOnInit() {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.isLoading.set(true);

    const currentUser = this.authService.user();
    if (currentUser) {
      this.userName.set(currentUser.fullName);
    }

    this.dashboardService.getDashboard().subscribe({
      next: (res: any) => {
        const data = res?.data || res;
        if (!data) {
          this.isLoading.set(false);
          return;
        }

        this.userName.set(data.userName || currentUser?.fullName || 'المعلم');
        this.todayClassesCount.set(data.todayClassesCount ?? 0);
        this.totalStudentsCount.set(data.totalStudentsCount ?? 0);
        this.pendingSubmissionsCount.set(data.pendingSubmissionsCount ?? 0);

        const colors = ['bg-primary', 'bg-secondary', 'bg-amber-600', 'bg-emerald-600', 'bg-indigo-600'];

        const mappedClasses = (data.classes || []).map((cls: any, index: number) => ({
          name: `${cls.className} - ${cls.subjectName}`,
          count: cls.studentCount || 0,
          classId: cls.classId,
          subjectId: cls.subjectId,
          classSubjectTeacherId: cls.classSubjectTeacherId,
          color: colors[index % colors.length]
        }));
        this.classes.set(mappedClasses);

        this.tasks.set(data.tasks || [
          'لا يوجد واجبات بانتظار التصحيح حالياً',
          'تحضير الحصص القادمة'
        ]);

        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }
}

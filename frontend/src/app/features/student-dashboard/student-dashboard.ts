import { Component, signal, OnInit, inject } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { AuthService } from '../../core/services/auth.service';
import { StudentDashboardService, StudentDashboardData } from './student-dashboard.service';
import { switchMap, of } from 'rxjs';

@Component({
  selector: 'app-student-dashboard',
  imports: [Sidebar, Topbar],
  templateUrl: './student-dashboard.html',
  styleUrl: './student-dashboard.css'
})
export class StudentDashboard implements OnInit {
  auth = inject(AuthService);
  private service = inject(StudentDashboardService);

  sidebarOpen = signal(false);

  data = signal<StudentDashboardData | null>(null);
  loading = signal(true);
  studentName = signal('');

  ngOnInit() {
    this.loading.set(true);

    this.service.get().pipe(
      switchMap(result => {
        if (!result) {
          this.studentName.set(this.auth.user()?.fullName ?? '');
          this.loading.set(false);
          return of(null);
        }
        this.studentName.set(result.student.fullName);
        return this.service.loadDetails(result.student.id, result.academicYearId);
      }),
      switchMap(enrollmentId => {
        if (!enrollmentId) {
          this.loading.set(false);
          return of(null);
        }
        return this.service.loadStats(enrollmentId);
      })
    ).subscribe(stats => {
      this.loading.set(false);
      if (stats) this.data.set(stats);
    });
  }

  get levelText(): string {
    const pct = this.data()?.overallPercentage ?? 0;
    if (pct >= 90) return 'ممتاز';
    if (pct >= 75) return 'جيد جداً';
    if (pct >= 60) return 'جيد';
    if (pct >= 50) return 'مقبول';
    return 'ضعيف';
  }
}

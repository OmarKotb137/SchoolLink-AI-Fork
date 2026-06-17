import { Component, signal, OnInit, inject } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { AuthService } from '../../core/services/auth.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
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
  private academicYearService = inject(AcademicYearService);

  sidebarOpen = signal(false);

  data = signal<StudentDashboardData | null>(null);
  loading = signal(true);
  studentName = signal('');
  selectedTerm = signal<number>(1);

  ngOnInit() {
    this.loadData();

    this.academicYearService.getCurrentTerm().subscribe({
      next: (res) => {
        if (res?.data != null && this.selectedTerm() !== res.data) {
          this.selectedTerm.set(res.data);
          this.loadData();
        }
      }
    });
  }

  loadData() {
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
        return this.service.loadStats(enrollmentId, this.selectedTerm());
      })
    ).subscribe(stats => {
      this.loading.set(false);
      if (stats) this.data.set(stats);
    });
  }

  onTermChange(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    this.selectedTerm.set(Number(value));
    this.loadData();
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

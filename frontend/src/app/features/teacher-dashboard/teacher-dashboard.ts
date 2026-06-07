import { Component, OnInit, signal, inject } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { AuthService } from '../../core/services/auth.service';
import { ClassService } from '../../core/services/class.service';
import { ClassSubjectTeacherService } from '../../core/services/class-subject-teacher.service';
import { TimetableService } from '../../core/services/timetable.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { AssignmentService } from '../../core/services/assignment.service';
import { forkJoin, of } from 'rxjs';
import { catchError, switchMap, map } from 'rxjs/operators';

@Component({
  selector: 'app-teacher-dashboard',
  imports: [Sidebar, Topbar],
  templateUrl: './teacher-dashboard.html',
  styleUrl: './teacher-dashboard.css'
})
export class TeacherDashboard implements OnInit {
  private authService = inject(AuthService);
  private classService = inject(ClassService);
  private classTeacherService = inject(ClassSubjectTeacherService);
  private timetableService = inject(TimetableService);
  private academicYearService = inject(AcademicYearService);
  private assignmentService = inject(AssignmentService);

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

    this.academicYearService.getCurrent().pipe(
      switchMap(yearRes => {
        const academicYearId = yearRes.data?.id || yearRes.id;
        if (!academicYearId) {
          return of({ yearId: null, classesRes: null, timetableRes: null, assignmentsRes: null });
        }

        const teacherId = currentUser?.userId;

        const classes$ = this.classTeacherService.getMyAssignmentsCurrentYear().pipe(
          catchError(() => of({ isSuccess: false, data: [] }))
        );

        const timetable$ = this.timetableService.getMyScheduleCurrentYear().pipe(
          catchError(() => of({ isSuccess: false, data: [] }))
        );

        const assignments$ = teacherId
          ? this.assignmentService.getByTeacher(teacherId, academicYearId).pipe(
              catchError(() => of({ isSuccess: false, data: [] }))
            )
          : of({ isSuccess: false, data: [] });

        return forkJoin({
          yearId: of(academicYearId),
          classesRes: classes$,
          timetableRes: timetable$,
          assignmentsRes: assignments$
        });
      }),
      switchMap((result: any) => {
        if (!result.yearId) {
          return of(null);
        }

        const classesData = result.classesRes?.data || result.classesRes || [];
        const timetableData = result.timetableRes?.data || result.timetableRes || [];
        const assignmentsData = result.assignmentsRes?.data || result.assignmentsRes || [];

        // JS DayOfWeek: 0 = Sunday, 1 = Monday, 2 = Tuesday, 3 = Wednesday, 4 = Thursday, 5 = Friday, 6 = Saturday
        // C# DayOfWeek: 0 = Sunday, 1 = Monday, 2 = Tuesday, 3 = Wednesday, 4 = Thursday, 5 = Friday, 6 = Saturday
        const todayDay = new Date().getDay();
        const todaySlots = timetableData.filter((slot: any) => slot.dayOfWeek === todayDay);
        this.todayClassesCount.set(todaySlots.length);

        const uniqueClassIds = Array.from(new Set(classesData.map((c: any) => c.classId))) as number[];

        const classStudents$ = uniqueClassIds.length > 0
          ? forkJoin(
              uniqueClassIds.map(classId =>
                this.classService.getStudents(classId).pipe(
                  map(res => ({ classId, count: res.data?.length || res?.length || 0 })),
                  catchError(() => of({ classId, count: 0 }))
                )
              )
            )
          : of([]);

        const activeAssignments = assignmentsData.filter((a: any) => a.id);
        const submissions$ = activeAssignments.length > 0
          ? forkJoin(
              activeAssignments.map((a: any) =>
                this.assignmentService.getSubmissionsByAssignment(a.id).pipe(
                  map(res => {
                    const subs = res.data || res || [];
                    const pendingGrading = subs.filter((s: any) => !s.isGraded).length;
                    return { assignmentId: a.id, title: a.title, pendingGrading };
                  }),
                  catchError(() => of({ assignmentId: a.id, title: a.title, pendingGrading: 0 }))
                )
              )
            )
          : of([]);

        return forkJoin({
          classesData: of(classesData),
          classStudents: classStudents$,
          submissions: submissions$
        });
      })
    ).subscribe({
      next: (finalRes: any) => {
        if (!finalRes) {
          this.isLoading.set(false);
          return;
        }

        const { classesData, classStudents, submissions } = finalRes;

        const studentCountMap = new Map<number, number>();
        let totalStudents = 0;
        classStudents.forEach((cs: any) => {
          studentCountMap.set(cs.classId, cs.count);
          totalStudents += cs.count;
        });
        this.totalStudentsCount.set(totalStudents);

        const colors = ['bg-primary', 'bg-secondary', 'bg-amber-600', 'bg-emerald-600', 'bg-indigo-600'];

        const mappedClasses = classesData.map((c: any, index: number) => {
          const count = studentCountMap.get(c.classId) || 0;
          return {
            name: `${c.className} - ${c.subjectName}`,
            count: count,
            color: colors[index % colors.length]
          };
        });
        this.classes.set(mappedClasses);

        let totalPendingGrading = 0;
        const pendingTasks: string[] = [];

        submissions.forEach((sub: any) => {
          totalPendingGrading += sub.pendingGrading;
          if (sub.pendingGrading > 0) {
            pendingTasks.push(`تصحيح واجب: ${sub.title} (${sub.pendingGrading} تسليمات بانتظار التقييم)`);
          }
        });

        this.pendingSubmissionsCount.set(totalPendingGrading);

        if (pendingTasks.length === 0) {
          pendingTasks.push('لا يوجد واجبات بانتظار التصحيح حالياً 👍');
          pendingTasks.push('تحضير الحصص القادمة');
        }
        this.tasks.set(pendingTasks);

        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading dashboard data', err);
        this.isLoading.set(false);
      }
    });
  }
}

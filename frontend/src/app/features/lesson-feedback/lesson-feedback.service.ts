import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, catchError, of } from 'rxjs';
import { buildApiUrl } from '../../core/utils/api-url';

export interface ClassSubjectTeacherDto {
  id: number;
  classId: number;
  className: string;
  subjectId: number;
  subjectName: string;
  teacherId: number;
  teacherName: string;
  academicYearId: number;
  academicYearName: string;
  weeklyPeriods: number;
}

export interface LessonFeedbackDto {
  id: number;
  enrollmentId: number;
  classSubjectTeacherId: number;
  teacherName: string;
  subjectName: string;
  lessonDate: string;
  rating: number;
  understanding: number;
  comment: string | null;
  createdAt: string;
}

export interface UnderstandingBreakdownDto {
  yesCount: number;
  partialCount: number;
  noCount: number;
}

export interface RatingTrendDto {
  date: string;
  averageRating: number;
  responseCount: number;
}

export interface FeedbackSummaryDto {
  averageRating: number;
  totalResponses: number;
  understandingBreakdown: UnderstandingBreakdownDto;
  ratingTrend: RatingTrendDto[];
}

export interface UserDto {
  id: number;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class LessonFeedbackService {
  private http = inject(HttpClient);

  getCurrentAcademicYear() {
    return this.http.get<any>(buildApiUrl('academic-years/current')).pipe(
      map(r => r.data),
      catchError(() => of(null)),
    );
  }

  getStudentByUserId(userId: number) {
    return this.http.get<any>(buildApiUrl(`students/by-user/${userId}`)).pipe(
      map(r => r.data),
      catchError(() => of(null)),
    );
  }

  getActiveEnrollment(studentId: number, academicYearId: number) {
    return this.http.get<any>(buildApiUrl(`enrollments/active/${studentId}?academicYearId=${academicYearId}`)).pipe(
      map(r => r.data),
      catchError(() => of(null)),
    );
  }

  getStudentSubjects(classId: number, academicYearId: number) {
    return this.http.get<any>(buildApiUrl(`class-subject-teachers/by-class-public/${classId}?academicYearId=${academicYearId}`)).pipe(
      map(r => r.data as ClassSubjectTeacherDto[]),
      catchError(() => of([] as ClassSubjectTeacherDto[])),
    );
  }

  getMyFeedback(enrollmentId: number) {
    return this.http.get<any>(buildApiUrl(`lesson-feedback/by-enrollment/${enrollmentId}`)).pipe(
      map(r => r.data as LessonFeedbackDto[]),
      catchError(() => of([] as LessonFeedbackDto[])),
    );
  }

  getFeedbackByLesson(cstId: number, lessonDate: string) {
    return this.http.get<any>(buildApiUrl(`lesson-feedback/by-lesson/${cstId}?lessonDate=${lessonDate}`)).pipe(
      map(r => r.data as LessonFeedbackDto[]),
      catchError(() => of([] as LessonFeedbackDto[])),
    );
  }

  submitFeedback(request: {
    enrollmentId: number;
    classSubjectTeacherId: number;
    lessonDate: string;
    rating: number;
    understanding: number;
    comment?: string;
  }) {
    return this.http.post<any>(buildApiUrl('lesson-feedback'), request).pipe(
      map(r => r.data as LessonFeedbackDto),
      catchError((err) => {
        const msg = err.error?.message || err.error?.title || 'حدث خطأ أثناء إرسال التقييم';
        throw new Error(msg);
      }),
    );
  }

  getMyAssignments(academicYearId?: number) {
    const query = academicYearId ? `?academicYearId=${academicYearId}` : '';
    return this.http.get<any>(buildApiUrl(`class-subject-teachers/my-assignments/current-year${query}`)).pipe(
      map(r => r.data as ClassSubjectTeacherDto[]),
      catchError(() => of([] as ClassSubjectTeacherDto[])),
    );
  }

  getFeedbackSummary(cstId: number, fromDate?: string, toDate?: string) {
    let query = '';
    if (fromDate) query += `?fromDate=${fromDate}`;
    if (toDate) query += `${query ? '&' : '?'}toDate=${toDate}`;
    return this.http.get<any>(buildApiUrl(`lesson-feedback/summary/${cstId}${query}`)).pipe(
      map(r => r.data as FeedbackSummaryDto),
      catchError(() => of(null)),
    );
  }

  getFeedbackRaw(cstId: number, fromDate?: string, toDate?: string) {
    let query = '';
    if (fromDate) query += `?fromDate=${fromDate}`;
    if (toDate) query += `${query ? '&' : '?'}toDate=${toDate}`;
    return this.http.get<any>(buildApiUrl(`lesson-feedback/raw/${cstId}${query}`)).pipe(
      map(r => r.data as LessonFeedbackDto[]),
      catchError(() => of([] as LessonFeedbackDto[])),
    );
  }

  updateFeedback(id: number, request: { rating: number; understanding: number; comment?: string }) {
    return this.http.put<any>(buildApiUrl(`lesson-feedback/${id}`), { id, ...request }).pipe(
      map(r => r.data as LessonFeedbackDto),
      catchError((err) => {
        const msg = err.error?.message || err.error?.title || 'حدث خطأ أثناء تحديث التقييم';
        throw new Error(msg);
      }),
    );
  }

  deleteFeedback(id: number, callerUserId: number) {
    return this.http.delete<any>(buildApiUrl(`lesson-feedback/${id}?callerUserId=${callerUserId}`)).pipe(
      catchError((err) => {
        const msg = err.error?.message || err.error?.title || 'حدث خطأ أثناء حذف التقييم';
        throw new Error(msg);
      }),
    );
  }

  getTeachers() {
    return this.http.get<any>(buildApiUrl('users/role/Teacher?pageSize=1000')).pipe(
      map(r => (r.data?.items || r.data) as UserDto[]),
      catchError(() => of([] as UserDto[])),
    );
  }

  getTeacherAssignments(teacherId: number, academicYearId: number) {
    return this.http.get<any>(buildApiUrl(`class-subject-teachers/by-teacher/${teacherId}?academicYearId=${academicYearId}`)).pipe(
      map(r => r.data as ClassSubjectTeacherDto[]),
      catchError(() => of([] as ClassSubjectTeacherDto[])),
    );
  }
}

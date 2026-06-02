import { Routes } from '@angular/router';

export const routes: Routes = [
  // Login pages
  { path: 'login', loadComponent: () => import('./pages/login-guardian/login-guardian').then(c => c.LoginGuardian) },
  { path: 'login-staff', loadComponent: () => import('./pages/login-staff/login-staff').then(c => c.LoginStaff) },
  { path: 'login-guardian', loadComponent: () => import('./pages/login-guardian/login-guardian').then(c => c.LoginGuardian) },

  // Dashboard / Feature pages
  { path: 'admin', loadComponent: () => import('./features/admin-dashboard/admin-dashboard').then(c => c.AdminDashboard) },
  { path: 'student', loadComponent: () => import('./features/student-dashboard/student-dashboard').then(c => c.StudentDashboard) },
  { path: 'teacher', loadComponent: () => import('./features/teacher-dashboard/teacher-dashboard').then(c => c.TeacherDashboard) },
  { path: 'parent', loadComponent: () => import('./features/parent-dashboard/parent-dashboard').then(c => c.ParentDashboard) },
  { path: 'class-analysis', loadComponent: () => import('./features/class-analysis/class-analysis').then(c => c.ClassAnalysis) },
  { path: 'admin-schedule', loadComponent: () => import('./features/admin-schedule/admin-schedule').then(c => c.AdminSchedule) },
  { path: 'class-schedule', loadComponent: () => import('./features/class-schedule/class-schedule').then(c => c.ClassSchedule) },
  { path: 'teacher-schedule', loadComponent: () => import('./features/teacher-schedule/teacher-schedule').then(c => c.TeacherSchedule) },
  { path: 'homework', loadComponent: () => import('./features/homework/homework').then(c => c.Homework) },
  { path: 'exam-generator', loadComponent: () => import('./features/exam-generator/exam-generator').then(c => c.ExamGenerator) },
  { path: 'lesson-creator', loadComponent: () => import('./features/lesson-creator/lesson-creator').then(c => c.LessonCreator) },
  { path: 'monthly-report', loadComponent: () => import('./features/monthly-report/monthly-report').then(c => c.MonthlyReport) },
  { path: 'study-planner', loadComponent: () => import('./features/study-planner/study-planner').then(c => c.StudyPlanner) },
  { path: 'chat', loadComponent: () => import('./features/chat/chat').then(c => c.Chat) },
  { path: 'chat-ai', loadComponent: () => import('./features/chat-ai/chat-ai').then(c => c.ChatAi) },
  { path: 'notifications', loadComponent: () => import('./features/notifications/notifications').then(c => c.Notifications) },
  { path: 'digital-library', loadComponent: () => import('./features/digital-library/digital-library').then(c => c.DigitalLibrary) },
  { path: 'reports', loadComponent: () => import('./features/reports/reports').then(c => c.Reports) },
  { path: 'reports-academic', loadComponent: () => import('./features/reports-academic/reports-academic').then(c => c.ReportsAcademic) },
  { path: 'reports-training', loadComponent: () => import('./features/reports-training/reports-training').then(c => c.ReportsTraining) },
  { path: 'analysis-ai', loadComponent: () => import('./features/analysis-ai/analysis-ai').then(c => c.AnalysisAi) },
  { path: 'settings', loadComponent: () => import('./features/settings/settings').then(c => c.Settings) },
  { path: 'grade-monitor', loadComponent: () => import('./features/grade-monitor/grade-monitor').then(c => c.GradeMonitor) },

  // Default redirect
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**', redirectTo: '/login' },
];

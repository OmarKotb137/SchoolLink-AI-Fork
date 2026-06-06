export interface SidebarMenuItem {
  label: string;
  icon: string;
  route: string;
}

export const ADMIN_MENU: SidebarMenuItem[] = [
  { label: 'لوحة القيادة', icon: 'dashboard', route: '/admin' },
  { label: 'تحليل الأداء', icon: 'analytics', route: '/analysis-ai' },
  { label: 'الجدول الدراسي', icon: 'calendar_month', route: '/admin-schedule' },
  { label: 'التعيينات', icon: 'assignment_ind', route: '/teacher-assignments' },
  { label: 'إدارة الفصول', icon: 'meeting_room', route: '/class-management' },
  { label: 'إدارة المواد', icon: 'menu_book', route: '/subject-management' },
  { label: 'إدارة القاعات', icon: 'door_open', route: '/room-management' },
  { label: 'التقارير', icon: 'description', route: '/reports' },
  { label: 'التقارير الأكاديمية', icon: 'auto_awesome', route: '/reports-academic' },
  { label: 'التقارير التدريبية', icon: 'model_training', route: '/reports-training' },
  { label: 'إدارة الامتحانات', icon: 'fact_check', route: '/exam-management' },
  { label: 'المكتبة الرقمية', icon: 'library_books', route: '/digital-library' },
  { label: 'المحادثات', icon: 'chat', route: '/chat' },
  { label: 'إدارة الحسابات', icon: 'manage_accounts', route: '/account-management' },
  { label: 'إدارة المعلمين', icon: 'person_add', route: '/teacher-management' },
  { label: 'إدارة الطلاب', icon: 'school', route: '/student-management' },
  { label: 'استيراد الطلاب', icon: 'file_upload', route: '/import-students' },
  { label: 'نقل طالب', icon: 'transfer_within_a_station', route: '/transfer-student' },
  { label: 'الإشعارات', icon: 'notifications', route: '/notifications' },
  { label: 'الإعدادات', icon: 'settings', route: '/settings' },
];

export const TEACHER_MENU: SidebarMenuItem[] = [
  { label: 'لوحة القيادة', icon: 'dashboard', route: '/teacher' },
  { label: 'فصولي', icon: 'groups', route: '/class-analysis' },
  { label: 'جدول الحصص', icon: 'calendar_month', route: '/teacher-schedule' },
  { label: 'إدارة الامتحانات', icon: 'fact_check', route: '/exam-management' },
  { label: 'إنشاء امتحان', icon: 'quiz', route: '/exam-generator' },
  { label: 'محتوى دروس', icon: 'menu_book', route: '/lesson-creator' },
  { label: 'إدارة الواجبات', icon: 'assignment_add', route: '/assignment-management' },
  { label: 'الواجبات', icon: 'assignment', route: '/homework' },
  { label: 'المحادثات', icon: 'chat', route: '/chat' },
  { label: 'رصد الدرجات', icon: 'grading', route: '/grade-monitor' },
  { label: 'المكتبة', icon: 'library_books', route: '/digital-library' },
  { label: 'التقارير', icon: 'description', route: '/reports' },
  { label: 'التقارير التدريبية', icon: 'model_training', route: '/reports-training' },
  { label: 'الإشعارات', icon: 'notifications', route: '/notifications' },
];

export const PARENT_MENU: SidebarMenuItem[] = [
  { label: 'لوحة القيادة', icon: 'dashboard', route: '/parent' },
  { label: 'جداول الأبناء', icon: 'calendar_month', route: '/parent-schedule' },
  { label: 'متابعة ابني', icon: 'supervisor_account', route: '/child-progress' },
  { label: 'التقارير', icon: 'description', route: '/reports' },
  { label: 'المحادثات', icon: 'chat', route: '/chat' },
  { label: 'المساعد الذكي', icon: 'auto_awesome', route: '/chat-ai' },
  { label: 'الإشعارات', icon: 'notifications', route: '/notifications' },
];

export const STUDENT_MENU: SidebarMenuItem[] = [
  { label: 'لوحة القيادة', icon: 'dashboard', route: '/student' },
  { label: 'الواجبات', icon: 'assignment', route: '/my-assignments' },
  { label: 'امتحاناتي', icon: 'quiz', route: '/my-exams' },
  { label: 'جدولي', icon: 'calendar_month', route: '/class-schedule' },
  { label: 'خطة المذاكرة', icon: 'calendar_view_month', route: '/study-planner' },
  { label: 'المحادثات', icon: 'chat', route: '/chat' },
  { label: 'المكتبة', icon: 'library_books', route: '/digital-library' },
  { label: 'التقارير', icon: 'description', route: '/reports' },
  { label: 'الإشعارات', icon: 'notifications', route: '/notifications' },
];

export const ROLE_MENUS: Record<string, SidebarMenuItem[]> = {
  admin: ADMIN_MENU,
  teacher: TEACHER_MENU,
  parent: PARENT_MENU,
  student: STUDENT_MENU,
};

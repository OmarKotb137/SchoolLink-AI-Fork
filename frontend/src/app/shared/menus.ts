export interface SidebarMenuItem {
  label: string;
  icon: string;
  route: string;
}

export const ADMIN_MENU: SidebarMenuItem[] = [
  { label: 'لوحة القيادة', icon: 'dashboard', route: '/admin' },
  { label: 'تحليل الأداء', icon: 'analytics', route: '/analysis-ai' },
  { label: 'الجدول الدراسي', icon: 'calendar_month', route: '/admin-schedule' },
  { label: 'التقارير', icon: 'description', route: '/reports' },
  { label: 'التقارير الأكاديمية', icon: 'auto_awesome', route: '/reports-academic' },
  { label: 'المكتبة الرقمية', icon: 'library_books', route: '/digital-library' },
  { label: 'الإشعارات', icon: 'notifications', route: '/notifications' },
  { label: 'الإعدادات', icon: 'settings', route: '/settings' },
];

export const TEACHER_MENU: SidebarMenuItem[] = [
  { label: 'لوحة القيادة', icon: 'dashboard', route: '/teacher' },
  { label: 'فصولي', icon: 'groups', route: '/class-analysis' },
  { label: 'جدول الحصص', icon: 'calendar_month', route: '/teacher-schedule' },
  { label: 'إنشاء امتحان', icon: 'quiz', route: '/exam-generator' },
  { label: 'محتوى دروس', icon: 'menu_book', route: '/lesson-creator' },
  { label: 'الواجبات', icon: 'assignment', route: '/homework' },
  { label: 'المحادثات', icon: 'chat', route: '/chat' },
  { label: 'رصد الدرجات', icon: 'grading', route: '/grade-monitor' },
  { label: 'المكتبة', icon: 'library_books', route: '/digital-library' },
  { label: 'التقارير', icon: 'description', route: '/reports' },
  { label: 'الإشعارات', icon: 'notifications', route: '/notifications' },
];

export const PARENT_MENU: SidebarMenuItem[] = [
  { label: 'لوحة القيادة', icon: 'dashboard', route: '/parent' },
  { label: 'التقارير', icon: 'description', route: '/reports' },
  { label: 'المحادثات', icon: 'chat', route: '/chat' },
  { label: 'المساعد الذكي', icon: 'auto_awesome', route: '/chat-ai' },
  { label: 'الإشعارات', icon: 'notifications', route: '/notifications' },
];

export const STUDENT_MENU: SidebarMenuItem[] = [
  { label: 'لوحة القيادة', icon: 'dashboard', route: '/student' },
  { label: 'الواجبات', icon: 'assignment', route: '/homework' },
  { label: 'جدولي', icon: 'calendar_month', route: '/class-schedule' },
  { label: 'خطة المذاكرة', icon: 'calendar_view_month', route: '/study-planner' },
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

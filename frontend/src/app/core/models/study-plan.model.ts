export interface StudyPlanDto {
  id: number;
  enrollmentId: number;
  generatedByAI: boolean;
  aiPromptSummary?: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  restDay: number | null;
  items: StudyPlanItemDto[];
  createdAt: string;
}

export interface StudyPlanItemDto {
  id: number;
  studyPlanId: number;
  subjectId: number;
  subjectName: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  topic?: string;
  notes?: string;
  isCompleted: boolean;
}

export interface StudyPlanSummaryDto {
  id: number;
  enrollmentId: number;
  generatedByAI: boolean;
  startDate: string;
  endDate: string;
  isActive: boolean;
  totalSessions: number;
  completedSessions: number;
  completionPercentage: number;
  createdAt: string;
}

export interface GenerateStudyPlanRequest {
  enrollmentId: number;
  startDate: string;
  endDate: string;
  aiPromptSummary?: string;
}

export interface CreateStudyPlanRequest {
  enrollmentId: number;
  startDate: string;
  endDate: string;
  restDay?: number | null;
  items: CreateStudyPlanItemRequest[];
}

export interface CreateStudyPlanItemRequest {
  subjectId: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  topic?: string;
  notes?: string;
}

export interface UpdateStudyPlanItemRequest {
  id: number;
  studyPlanId: number;
  subjectId?: number | null;
  dayOfWeek?: number | null;
  startTime?: string | null;
  endTime?: string | null;
  topic?: string | null;
  notes?: string | null;
}

export interface StudyPlanOptimizationRequest {
  enrollmentId: number;
  availableDays: number;
  hoursPerDay: number;
  weakSubjects: string[];
  startDate?: string | null;
  endDate?: string | null;
}

export interface SessionCell {
  id: number;
  subjectName: string;
  topic: string;
  startTime: string;
  endTime: string;
  isCompleted: boolean;
  notes?: string;
}

export const DAY_NAMES = ['السبت', 'الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة'];

export const SCHOOL_DAY_TO_GRID: Record<string, number> = {
  'Saturday': 0, 'Sunday': 1, 'Monday': 2, 'Tuesday': 3, 'Wednesday': 4, 'Thursday': 5, 'Friday': 6,
};

export const GRID_TO_SCHOOL_DAY: Record<number, string> = {
  0: 'Saturday', 1: 'Sunday', 2: 'Monday', 3: 'Tuesday', 4: 'Wednesday', 5: 'Thursday', 6: 'Friday',
};
export const PERIOD_NAMES = [
  { label: 'الصباح', icon: 'wb_sunny', color: '#2563eb', start: '08:00', end: '12:00' },
  { label: 'الظهر', icon: 'light_mode', color: '#1d4ed8', start: '12:00', end: '16:00' },
  { label: 'المساء', icon: 'dark_mode', color: '#3b82f6', start: '16:00', end: '20:00' },
  { label: 'الليل', icon: 'nights_stay', color: '#6b7280', start: '20:00', end: '22:00' },
];
export interface ClassAnalysisOverview {
  classId: number;
  className: string;
  gradeLevelName: string;
  totalStudents: number;
  classAverage: number;
  classAverageChange: number;
  maxScore: number;
  topStudentsCount: number;
  atRiskStudentsCount: number;
  attendanceRate: number;
}

export interface SubjectPerformance {
  subjectId: number;
  subjectName: string;
  classAverage: number;
  schoolAverage: number;
  maxScore: number;
  difference: number;
}

export interface AttendanceTrend {
  month: string;
  monthNumber: number;
  year: number;
  attendanceRate: number;
  absenceRate: number;
  totalSchoolDays: number;
  absenceDays: number;
}

export interface TopStudent {
  studentId: number;
  studentName: string;
  averageScore: number;
  maxScore: number;
  rank: number;
  photoUrl?: string;
}

export interface AtRiskStudent {
  studentId: number;
  studentName: string;
  averageScore: number;
  maxScore: number;
  attendanceRate: number;
  weakSubjects: string[];
  severity: 'warning' | 'danger' | 'critical';
}

export interface Weakness {
  skillName: string;
  subjectId: number;
  subjectName: string;
  severity: 'safe' | 'low' | 'medium' | 'critical';
  averageScore: number;
  maxScore: number;
}

export interface ClassStudent {
  studentId: number;
  studentName: string;
  averageScore: number;
  attendanceRate: number;
  absenceCount: number;
  status: 'active' | 'at-risk' | 'excellent';
}

export interface ClassAnalysisFull {
  overview: ClassAnalysisOverview;
  subjectPerformance: SubjectPerformance[];
  attendanceTrends: AttendanceTrend[];
  topStudents: TopStudent[];
  atRiskStudents: AtRiskStudent[];
  weaknessAnalysis: Weakness[];
  students: ClassStudent[];
}

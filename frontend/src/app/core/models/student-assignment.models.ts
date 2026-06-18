export type StudentAssignmentStatus =
  | 'pending'
  | 'late'
  | 'submittedWaitingGrade'
  | 'graded';

export enum StudentAssignmentQuestionType {
  MultipleChoice = 'MultipleChoice',
  TrueFalse = 'TrueFalse',
  FillBlank = 'FillBlank',
  Essay = 'Essay'
}


export interface StudentAssignmentListItem {
  assignmentId: number;
  title: string;
  subjectName: string;
  className: string;
  dueDate?: string | null;
  maxScore: number;
  questionsCount: number;
  status: StudentAssignmentStatus;
  submissionId?: number | null;
  submittedAt?: string | null;
  isGraded: boolean;
  score?: number | null;
}

export interface StudentAssignmentDetails {
  assignmentId: number;
  title: string;
  description?: string | null;
  subjectName: string;
  className: string;
  dueDate?: string | null;
  maxScore: number;
  isAutoGraded: boolean;
  status: StudentAssignmentStatus;
  submissionId?: number | null;
  questions: StudentAssignmentQuestion[];
}

export interface StudentAssignmentQuestion {
  id: number;
  questionText: string;
  questionType: StudentAssignmentQuestionType;
  imageUrl?: string | null;
  points: number;
  displayOrder: number;
  options: StudentAssignmentQuestionOption[];
}

export interface StudentAssignmentQuestionOption {
  id: number;
  optionText: string;
  displayOrder: number;
}

export interface StudentAssignmentAnswerPayload {
  questionId: number;
  answerText?: string | null;
  selectedOptionId?: number | null;
  booleanAnswer?: boolean | null;
}

export interface StudentAssignmentDraft {
  assignmentId: number;
  updatedAt: string;
  answers: StudentAssignmentAnswerPayload[];
}

export interface StudentAssignmentSubmissionResult {
  submissionId: number;
  assignmentId: number;
  isSubmitted: boolean;
  isGraded: boolean;
  score?: number | null;
  maxScore: number;
  submittedAt?: string | null;
  message: string;
  answers: StudentAssignmentResultAnswer[];
}

export interface StudentAssignmentResultAnswer {
  questionId: number;
  questionText: string;
  answerText?: string | null;
  selectedOptionId?: number | null;
  booleanAnswer?: boolean | null;
  isCorrect?: boolean | null;
  pointsEarned: number;
  questionPoints: number;
  aiFeedback?: string | null;
}

export type StudentExamStatus =
  | 'upcoming'
  | 'available'
  | 'inProgress'
  | 'submittedWaitingGrade'
  | 'gradedHidden'
  | 'resultVisible'
  | 'expired';

export enum StudentQuestionType {
  MultipleChoice = 'MultipleChoice',
  TrueFalse = 'TrueFalse',
  FillBlank = 'FillBlank',
  Essay = 'Essay'
}


export interface StudentExamListItem {
  examId: number;
  title: string;
  subjectName: string;
  startTime?: string | null;
  endTime?: string | null;
  durationMinutes?: number | null;
  totalScore: number;
  questionsCount: number;
  status: StudentExamStatus;
  attemptId?: number | null;
  startedAt?: string | null;
  submittedAt?: string | null;
  isGraded: boolean;
  isResultPublished: boolean;
  score?: number | null;
}

export interface StudentExamDetails {
  examId: number;
  title: string;
  subjectName: string;
  className: string;
  startTime?: string | null;
  endTime?: string | null;
  durationMinutes?: number | null;
  totalScore: number;
  status: StudentExamStatus;
  questions: StudentExamQuestion[];
}

export interface StudentExamQuestion {
  id: number;
  questionText: string;
  questionType: StudentQuestionType;
  contentText?: string | null;
  imageUrl?: string | null;
  points: number;
  displayOrder: number;
  options: StudentExamQuestionOption[];
}

export interface StudentExamQuestionOption {
  id: number;
  optionText: string;
  displayOrder: number;
}

export interface StudentExamAttemptStarted {
  attemptId: number;
  examId: number;
  startedAt: string;
  serverNow: string;
  durationMinutes?: number | null;
  endsAt?: string | null;
}

export interface StudentExamAnswerPayload {
  questionId: number;
  answerText?: string | null;
  selectedOptionId?: number | null;
  booleanAnswer?: boolean | null;
}

export interface SaveAnswerProgressPayload {
  questionId: number;
  answerText?: string | null;
  selectedOptionId?: number | null;
  booleanAnswer?: boolean | null;
}

export type SaveStatus = 'idle' | 'saving' | 'saved' | 'failed';

export interface StudentExamDraft {
  attemptId: number;
  examId: number;
  updatedAt: string;
  answers: StudentExamAnswerPayload[];
}

export interface StudentExamAttemptResult {
  attemptId: number;
  isSubmitted: boolean;
  isGraded: boolean;
  isResultPublished: boolean;
  score?: number | null;
  totalScore: number;
  message: string;
  answers: StudentExamResultAnswer[];
}

export interface StudentExamResultAnswer {
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

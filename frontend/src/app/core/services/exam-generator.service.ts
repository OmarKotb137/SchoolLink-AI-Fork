import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface AiGenerateExamRequest {
  classSubjectTeacherId?: number | null;
  title: string;
  durationMinutes?: number;
  totalScore: number;
  category: number;
  questionCounts: Record<number, number>;
  topic?: string;
  unitId?: number;
  lessonIds: number[];
}

export interface GetExamDto {
  id: number;
  uid: string;
  title: string;
  durationMinutes: number | null;
  totalScore: number;
  isAIGenerated: boolean;
  isPublished: boolean;
  category: number;
  subjectName: string;
  className: string;
  teacherName: string;
  questionsCount: number;
  createdAt: string;
  groups: GetExamQuestionGroupDto[];
  standaloneQuestions: GetExamQuestionDto[];
}

export interface GetExamQuestionGroupDto {
  id: number;
  displayType: number;
  contentTitle?: string;
  contentText?: string;
  imageUrl?: string;
  displayOrder: number;
  questions: GetExamQuestionDto[];
}

export interface GetExamQuestionDto {
  id: number;
  groupId?: number;
  displayType: number;
  contentText?: string;
  questionText: string;
  questionType: number;
  correctAnswer: string | null;
  imageUrl?: string;
  points: number;
  displayOrder: number;
  options: GetExamQuestionOptionDto[];
}

export interface GetExamQuestionOptionDto {
  id: number;
  optionText: string;
  isCorrect: boolean;
  displayOrder: number;
}

export interface AiExamPreviewDto {
  subjectName: string;
  className: string;
  teacherName: string;
  title: string;
  durationMinutes: number | null;
  totalScore: number;
  questionsCount: number;
  standaloneQuestions: AiExamPreviewQuestionDto[];
}

export interface AiExamPreviewQuestionDto {
  questionText: string;
  questionType: number;
  options: AiExamPreviewOptionDto[] | null;
  correctAnswer: string | null;
  points: number;
  displayOrder: number;
}

export interface AiExamPreviewOptionDto {
  optionText: string;
  isCorrect: boolean;
  displayOrder: number;
}

export interface ExamSummaryDto {
  id: number;
  title: string;
  startTime: string | null;
  endTime: string | null;
  totalScore: number;
  isPublished: boolean;
  isAIGenerated: boolean;
  category: number;
  subjectName: string;
  questionsCount: number;
}

@Injectable({ providedIn: 'root' })
export class ExamGeneratorService {
  private http = inject(HttpClient);
  private base = buildApiUrl('ai/exam-generator');
  private cstBase = buildApiUrl('class-subject-teachers');
  private unitBase = buildApiUrl('subjects');
  private examBase = buildApiUrl('exam');

  aiGenerate(body: AiGenerateExamRequest): Observable<any> {
    return this.http.post(`${this.base}/ai-generate`, body);
  }

  preview(body: AiGenerateExamRequest): Observable<any> {
    return this.http.post(`${this.base}/preview`, body);
  }

  save(body: any): Observable<any> {
    return this.http.post(`${this.base}/save`, body);
  }

  getHistory(): Observable<any> {
    return this.http.get(`${this.base}/history`);
  }

  getMyAssignments(): Observable<any> {
    return this.http.get(`${this.cstBase}/my-assignments/current-year`);
  }

  getUnitsWithLessons(subjectId: number): Observable<any> {
    return this.http.get(`${this.unitBase}/${subjectId}/units-with-lessons`);
  }

  getExamById(id: number): Observable<any> {
    return this.http.get(`${this.examBase}/${id}`);
  }

  deleteExamById(id: number): Observable<any> {
    return this.http.delete(`${this.examBase}/${id}`);
  }

  saveExistingExam(uid: string, dto: any): Observable<any> {
    return this.http.put(`${this.examBase}/${uid}/save`, dto);
  }
}

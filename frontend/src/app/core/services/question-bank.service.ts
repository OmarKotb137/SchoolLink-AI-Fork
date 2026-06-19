import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface QuestionBankItemDto {
  id: number;
  questionText: string;
  questionType: number;
  correctAnswer: string | null;
  options: QuestionBankOptionDto[];
  subjectName: string;
  subjectId: number;
  gradeLevelId: number;
  gradeLevelName: string | null;
  usageCount: number;
  createdAt: string;
}

export interface QuestionBankOptionDto {
  optionText: string;
  isCorrect: boolean;
  displayOrder: number;
}

export interface SearchQuestionBankDto {
  searchText?: string;
  subjectId?: number;
  gradeLevelId?: number;
  questionType?: number;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class QuestionBankService {
  private http = inject(HttpClient);
  private base = buildApiUrl('QuestionBank');

  getBySubject(subjectId: number, gradeLevelId?: number): Observable<any> {
    let url = `${this.base}/subject/${subjectId}`;
    if (gradeLevelId) url += `?gradeLevelId=${gradeLevelId}`;
    return this.http.get(url);
  }

  getById(id: number): Observable<any> {
    return this.http.get(`${this.base}/${id}`);
  }

  search(dto: SearchQuestionBankDto): Observable<any> {
    return this.http.post(`${this.base}/search`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.base}/${id}`);
  }

  add(dto: AddQuestionDto): Observable<any> {
    return this.http.post(`${this.base}`, dto);
  }

  update(id: number, dto: AddQuestionDto): Observable<any> {
    return this.http.put(`${this.base}/${id}`, dto);
  }
}

export interface AddQuestionDto {
  questionText: string;
  questionType: number;
  correctAnswer?: string | null;
  options: AddOptionDto[];
  subjectId: number;
  gradeLevelId: number;
}

export interface AddOptionDto {
  text: string;
  isCorrect: boolean;
  displayOrder: number;
}

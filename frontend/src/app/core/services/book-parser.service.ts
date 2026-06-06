import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OperationResult } from '../models/library.model';

export interface ParsedLessonDto {
  title: string;
  pageStart: number | null;
  pageEnd: number | null;
  displayOrder: number;
}

export interface ParsedUnitDto {
  name: string;
  content: string;
  pageStart: number | null;
  pageEnd: number | null;
  displayOrder: number;
  lessons: ParsedLessonDto[];
}

export interface CreateUnitDto {
  name: string;
  content: string;
  pageStart: number | null;
  pageEnd: number | null;
  displayOrder: number;
  lessons: {
    title: string;
    pageStart: number | null;
    pageEnd: number | null;
    displayOrder: number;
  }[];
}

export interface UnitDto {
  id: number;
  subjectId: number;
  name: string;
  content: string;
  pageStart: number | null;
  pageEnd: number | null;
  displayOrder: number;
  lessons: {
    id: number;
    title: string;
    pageStart: number | null;
    pageEnd: number | null;
    displayOrder: number;
  }[];
}

@Injectable({ providedIn: 'root' })
export class BookParserService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  preview(file: File): Observable<OperationResult<ParsedUnitDto[]>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<OperationResult<ParsedUnitDto[]>>(`${this.base}/book-parser/preview`, formData);
  }

  save(subjectId: number, units: CreateUnitDto[]): Observable<OperationResult<UnitDto[]>> {
    return this.http.post<OperationResult<UnitDto[]>>(
      `${this.base}/book-parser/save?subjectId=${subjectId}`, units);
  }

  getGradeLevels(): Observable<any> {
    return this.http.get(`${this.base}/grade-levels`);
  }

  getSubjects(gradeLevelId?: number): Observable<any> {
    if (gradeLevelId) {
      return this.http.get(`${this.base}/subjects/by-grade-level/${gradeLevelId}`);
    }
    return this.http.get(`${this.base}/subjects`);
  }
}
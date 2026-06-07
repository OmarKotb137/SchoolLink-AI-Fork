import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
export interface ParsedLessonDto {
  title: string;
  content?: string;
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
    content?: string;
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
    content?: string;
    pageStart: number | null;
    pageEnd: number | null;
    displayOrder: number;
  }[];
}

@Injectable({ providedIn: 'root' })
export class BookParserService {
  private http = inject(HttpClient);
  private parserBase = buildApiUrl('book-parser');

  preview(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${this.parserBase}/preview`, formData);
  }

  save(subjectId: number, units: CreateUnitDto[]): Observable<any> {
    return this.http.post<any>(
      `${this.parserBase}/save?subjectId=${subjectId}`, units);
  }

  generateLessonContent(rawContent: string, title: string): Observable<any> {
    return this.http.post<any>(`${this.parserBase}/lesson/generate-content`, {
      title,
      rawContent
    });
  }

  reExtractLessonContent(previewId: string, lessonTitle: string, pageStart: number, pageEnd: number | null): Observable<any> {
    return this.http.post<any>(`${this.parserBase}/lesson/re-extract`, {
      previewId,
      lessonTitle,
      pageStart,
      pageEnd
    });
  }

  reExtractUnitContent(previewId: string, unitName: string, pageStart: number, pageEnd: number | null): Observable<any> {
    return this.http.post<any>(`${this.parserBase}/unit/re-extract`, {
      previewId,
      unitName,
      pageStart,
      pageEnd
    });
  }

  getParsedSubjects(): Observable<any> {
    return this.http.get<any>(`${this.parserBase}/subjects`);
  }

  getSubjectStructure(subjectId: number): Observable<any> {
    return this.http.get<any>(`${this.parserBase}/subjects/${subjectId}`);
  }

  updateUnit(unitId: number, name: string, content?: string): Observable<any> {
    const body: any = { name };
    if (content !== undefined) body.content = content;
    return this.http.put<any>(`${this.parserBase}/units/${unitId}`, body);
  }

  updateLesson(lessonId: number, title: string): Observable<any> {
    return this.http.put<any>(`${this.parserBase}/lessons/${lessonId}`, { title });
  }

  createLesson(unitId: number, dto: { title: string; displayOrder: number }): Observable<any> {
    return this.http.post<any>(`${this.parserBase}/units/${unitId}/lessons`, dto);
  }

  createUnit(subjectId: number, dto: { name: string; displayOrder: number }): Observable<any> {
    return this.http.post<any>(`${this.parserBase}/subjects/${subjectId}/units`, dto);
  }

  getGradeLevels(): Observable<any> {
    return this.http.get(buildApiUrl('grade-levels'));
  }

  getSubjects(gradeLevelId?: number): Observable<any> {
    if (gradeLevelId) {
      return this.http.get(buildApiUrl(`subjects/by-grade-level/${gradeLevelId}`));
    }
    return this.http.get(buildApiUrl('subjects'));
  }
}
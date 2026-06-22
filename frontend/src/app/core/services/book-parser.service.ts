import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
export interface ParsedLessonDto {
  id?: number;
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
  gradeLevelId?: number;
  term?: number | null;
  lessons: ParsedLessonDto[];
}

export interface CreateUnitDto {
  gradeLevelId: number;
  name: string;
  content: string;
  pageStart: number | null;
  pageEnd: number | null;
  displayOrder: number;
  term?: number | null;
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
  gradeLevelId: number;
  name: string;
  content: string;
  pageStart: number | null;
  pageEnd: number | null;
  displayOrder: number;
  subjectName?: string;
  gradeLevelName?: string;
  term?: number | null;
  lessons: {
    id: number;
    title: string;
    content?: string;
    pageStart: number | null;
    pageEnd: number | null;
    displayOrder: number;
  }[];
}

export interface SubjectWithStructureDto {
  id: number;
  name: string;
  gradeLevelId: number;
  gradeLevelName?: string;
  unitCount: number;
  lessonCount: number;
  term?: number | null;
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

  save(subjectId: number, gradeLevelId: number, units: CreateUnitDto[], term?: number | null): Observable<any> {
    let url = `${this.parserBase}/save?subjectId=${subjectId}&gradeLevelId=${gradeLevelId}`;
    if (term != null) url += `&term=${term}`;
    return this.http.post<any>(url, units);
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

  getParsedSubjects(term?: number | null): Observable<any> {
    let url = `${this.parserBase}/subjects`;
    if (term != null) url += `?term=${term}`;
    return this.http.get<any>(url);
  }

  getSubjectStructure(subjectId: number, term?: number | null): Observable<any> {
    let url = `${this.parserBase}/subjects/${subjectId}`;
    if (term != null) url += `?term=${term}`;
    return this.http.get<any>(url);
  }

  updateUnit(unitId: number, name: string, content?: string): Observable<any> {
    const body: any = { name };
    if (content !== undefined) body.content = content;
    return this.http.put<any>(`${this.parserBase}/units/${unitId}`, body);
  }

  updateLesson(lessonId: number, title: string, content?: string, pageStart?: number | null, pageEnd?: number | null): Observable<any> {
    const body: any = { title };
    if (content !== undefined) body.content = content;
    if (pageStart !== undefined) body.pageStart = pageStart;
    if (pageEnd !== undefined) body.pageEnd = pageEnd;
    return this.http.put<any>(`${this.parserBase}/lessons/${lessonId}`, body);
  }

  createLesson(unitId: number, dto: { title: string; displayOrder: number }): Observable<any> {
    return this.http.post<any>(`${this.parserBase}/units/${unitId}/lessons`, dto);
  }

  createUnit(subjectId: number, dto: { name: string; displayOrder: number }): Observable<any> {
    return this.http.post<any>(`${this.parserBase}/subjects/${subjectId}/units`, dto);
  }

  deleteUnit(unitId: number): Observable<any> {
    return this.http.delete<any>(`${this.parserBase}/units/${unitId}`);
  }

  deleteLesson(lessonId: number): Observable<any> {
    return this.http.delete<any>(`${this.parserBase}/lessons/${lessonId}`);
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

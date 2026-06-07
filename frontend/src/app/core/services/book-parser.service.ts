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
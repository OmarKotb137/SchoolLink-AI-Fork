import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { GetLibraryFilter, LibraryItemDto, OperationResult, PagedResult } from '../models/library.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LibraryService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/api`;

  // --- Library Endpoints ---

  getAll(filter: GetLibraryFilter) {
    let params = new HttpParams()
      .set('page', filter.page.toString())
      .set('pageSize', filter.pageSize.toString());

    if (filter.subjectId) params = params.set('subjectId', filter.subjectId.toString());
    if (filter.gradeLevelId) params = params.set('gradeLevelId', filter.gradeLevelId.toString());
    if (filter.academicYearId) params = params.set('academicYearId', filter.academicYearId.toString());
    if (filter.itemType) params = params.set('itemType', filter.itemType.toString());
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);

    return this.http.get<OperationResult<PagedResult<LibraryItemDto>>>(`${this.base}/Library`, { params });
  }

  getLatest(count: number = 5) {
    return this.http.get<OperationResult<LibraryItemDto[]>>(`${this.base}/Library/latest?count=${count}`);
  }

  getById(id: number) {
    return this.http.get<OperationResult<LibraryItemDto>>(`${this.base}/Library/${id}`);
  }

  search(term: string, gradeLevelId: number) {
    return this.http.get<OperationResult<LibraryItemDto[]>>(`${this.base}/Library/search?term=${encodeURIComponent(term)}&gradeLevelId=${gradeLevelId}`);
  }

  // --- Subjects (for filters) ---
  getSubjects() {
    return this.http.get<any>(`${this.base}/Subjects`);
  }

  // --- Upload ---
  upload(
    file: File | null,
    linkUrl: string | null,
    title: string,
    itemType: number,
    subjectId: number | null,
    gradeLevelId: number | null,
    academicYearId: number | null,
    description?: string
  ) {
    const formData = new FormData();
    if (file) {
      formData.append('file', file);
    }
    if (linkUrl) {
      formData.append('linkUrl', linkUrl);
    }
    formData.append('title', title);
    formData.append('itemType', itemType.toString());

    if (subjectId) formData.append('subjectId', subjectId.toString());
    if (gradeLevelId) formData.append('gradeLevelId', gradeLevelId.toString());
    if (academicYearId) formData.append('academicYearId', academicYearId.toString());
    if (description) formData.append('description', description);

    return this.http.post<OperationResult<LibraryItemDto>>(`${this.base}/Library/upload`, formData);
  }

  delete(id: number) {
    return this.http.delete<void>(`${this.base}/Library/${id}`);
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface ImportedStudent {
  id: number;
  fullName: string;
  nationalId?: string | null;
  gender?: string | null;
  birthDate?: string | null;
}

export interface ClassInfo {
  id: number;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class StudentImportService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('ai/student-import');

  preview(files: FileList): Observable<any> {
    const fd = new FormData();
    for (let i = 0; i < files.length; i++) {
      fd.append('files', files[i]);
    }
    return this.http.post(`${this.apiUrl}/preview`, fd);
  }

  import(students: ImportedStudent[], classId: number, academicYearId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/import`, { students, classId, academicYearId });
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult } from '../models/api.model';

export interface User {
  id: number;
  fullName: string;
  username: string;
  contactEmail?: string | null;
  isContactEmailVerified?: boolean;
  contactEmailVerifiedAt?: string | null;
  email?: string;
  role: string;
  phone?: string;
  isActive: boolean;
  profilePictureUrl?: string;
  createdAt?: string;
}

export interface GetUsersFilter {
  role?: 'Admin' | 'Teacher' | 'Parent' | 'Student';
  isActive?: boolean;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateUserRequest {
  fullName: string;
  username: string;
  contactEmail?: string;
  password: string;
  phone?: string;
  role: string;
}

export interface UpdateUserRequest {
  fullName: string;
  phone?: string;
  profilePictureUrl?: string;
}

export interface UpdateProfileRequest {
  fullName: string;
  phone?: string;
}

export interface StudentAccountCandidate {
  studentId: number;
  fullName: string;
  nationalId?: string | null;
  gender?: string | null;
  createdAt?: string;
}

export interface GenerateStudentAccountResult {
  studentId: number;
  studentName: string;
  generatedUsername: string;
  plainPassword: string;
  success: boolean;
  errorMessage?: string | null;
}

export interface GenerateBulkStudentAccountsResult {
  totalRequested: number;
  successCount: number;
  failureCount: number;
  results: GenerateStudentAccountResult[];
}

export interface ParentChildLinkRequest {
  studentId: number;
  relationship: number;
}

export interface CreateParentWithStudentsRequest {
  fullName: string;
  username?: string;
  contactEmail?: string;
  password: string;
  phone?: string;
  children: ParentChildLinkRequest[];
}

export interface CreateParentWithStudentsResult {
  parent: User;
  linkedCount: number;
  failedCount: number;
  linkResults: Array<{
    studentId: number;
    studentName: string;
    success: boolean;
    errorMessage?: string | null;
  }>;
}

export interface ResetPasswordResult {
  userId: number;
  fullName: string;
  newPassword: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('Users');
  private profileUrl = buildApiUrl('Profile');
  private accountGenUrl = buildApiUrl('account-generation');

  getAll(filter?: GetUsersFilter): Observable<any> {
    let params = new HttpParams();

    if (filter?.role) params = params.set('role', filter.role);
    if (filter?.isActive !== undefined) params = params.set('isActive', filter.isActive);
    if (filter?.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter?.page) params = params.set('page', filter.page);
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize);

    return this.http.get<any>(this.apiUrl, { params });
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  search(term: string, pageSize: number = 20): Observable<any> {
    const params = new HttpParams().set('term', term).set('pageSize', pageSize.toString());
    return this.http.get<any>(`${this.apiUrl}/search`, { params });
  }

  getByRole(role: string, pageSize: number = 1000): Observable<any> {
    const params = new HttpParams().set('pageSize', pageSize.toString());
    return this.http.get<any>(`${this.apiUrl}/role/${role}`, { params });
  }

  createUser(data: CreateUserRequest): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  updateUser(id: number, data: UpdateUserRequest): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  setActiveStatus(id: number, isActive: boolean): Observable<any> {
    return this.http.patch<any>(`${this.apiUrl}/${id}/active-status`, isActive);
  }

  deleteUser(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

  getStudentAccountCandidates(): Observable<OperationResult<StudentAccountCandidate[]>> {
    return this.http.get<OperationResult<StudentAccountCandidate[]>>(`${this.accountGenUrl}/student-candidates`);
  }

  generateStudentAccount(studentId: number): Observable<OperationResult<GenerateStudentAccountResult>> {
    return this.http.post<OperationResult<GenerateStudentAccountResult>>(`${this.accountGenUrl}/students/generate`, { studentId });
  }

  generateBulkStudentAccounts(studentIds: number[]): Observable<OperationResult<GenerateBulkStudentAccountsResult>> {
    return this.http.post<OperationResult<GenerateBulkStudentAccountsResult>>(`${this.accountGenUrl}/students/generate-bulk`, { studentIds });
  }

  createParentWithStudents(data: CreateParentWithStudentsRequest): Observable<OperationResult<CreateParentWithStudentsResult>> {
    return this.http.post<OperationResult<CreateParentWithStudentsResult>>(`${this.accountGenUrl}/parents/create-with-students`, data);
  }

  checkParentPhone(phone: string): Observable<OperationResult<{ alreadyExists: boolean; existingParentId?: number | null; existingParentName?: string | null; existingParentUsername?: string | null }>> {
    const params = new HttpParams().set('phone', phone);
    return this.http.get<OperationResult<{ alreadyExists: boolean; existingParentId?: number | null; existingParentName?: string | null; existingParentUsername?: string | null }>>(`${this.accountGenUrl}/parents/check-phone`, { params });
  }

  resetPassword(userId: number): Observable<OperationResult<ResetPasswordResult>> {
    return this.http.post<OperationResult<ResetPasswordResult>>(`${this.apiUrl}/${userId}/reset-password`, {});
  }

  getMyProfile(): Observable<OperationResult<User>> {
    return this.http.get<OperationResult<User>>(`${this.profileUrl}/me`);
  }

  updateMyProfile(data: UpdateProfileRequest): Observable<OperationResult<User>> {
    return this.http.put<OperationResult<User>>(this.profileUrl, data);
  }

  sendEmailOtp(email: string): Observable<OperationResult<unknown>> {
    return this.http.post<OperationResult<unknown>>(`${this.profileUrl}/email/send-otp`, { email });
  }

  verifyEmailOtp(email: string, code: string): Observable<OperationResult<unknown>> {
    return this.http.post<OperationResult<unknown>>(`${this.profileUrl}/email/verify-otp`, { email, code });
  }

  uploadProfilePhoto(userId: number, file: File): Observable<{ photoUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ photoUrl: string }>(`${this.profileUrl}/upload-photo/${userId}`, formData);
  }

  deleteProfilePhoto(): Observable<OperationResult<unknown>> {
    return this.http.delete<OperationResult<unknown>>(`${this.profileUrl}/photo`);
  }
}

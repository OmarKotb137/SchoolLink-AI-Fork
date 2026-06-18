import { Component, signal, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { AuthService } from '../../core/services/auth.service';
import { buildApiUrl } from '../../core/utils/api-url';
import { ParentDashboardService, ParentDashboardChild } from '../../core/services/parent-dashboard.service';
import { map } from 'rxjs';

interface AIReportResult {
  id: number;
  studentId: number;
  periodId?: number;
  classId?: number;
  term?: number;
  reportType: string;
  content: string;
  summary?: string;
  createdAt: string;
}

@Component({
  selector: 'app-reports',
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './reports.html',
  styleUrl: './reports.css',
})
export class Reports implements OnInit {
  private authService = inject(AuthService);
  private http = inject(HttpClient);
  private parentDashboardService = inject(ParentDashboardService);

  sidebarOpen = signal(false);
  loading = signal(false);
  generating = signal(false);
  error = signal<string | null>(null);

  // Children
  children = signal<ParentDashboardChild[]>([]);
  selectedChildId = signal<number | null>(null);
  selectedChildName = signal('');

  // Report data
  reportContent = signal<string | null>(null);
  reportHistory = signal<AIReportResult[]>([]);
  recommendations = signal<string | null>(null);

  private aiBase = buildApiUrl('ai/reports');

  ngOnInit() {
    this.loadChildren();
  }

  loadChildren() {
    this.loading.set(true);
    this.parentDashboardService.getMyChildren().pipe(
      map((res: any) => res?.data ?? res ?? [])
    ).subscribe({
      next: (data: ParentDashboardChild[]) => {
        const items = Array.isArray(data) ? data : [];
        this.children.set(items);
        if (items.length > 0) {
          this.selectedChildId.set(items[0].studentId);
          this.selectedChildName.set(items[0].studentName);
          this.loadReports(items[0].studentId);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  selectChild(studentId: number, name: string) {
    this.selectedChildId.set(studentId);
    this.selectedChildName.set(name);
    this.loadReports(studentId);
  }

  loadReports(studentId: number) {
    this.loading.set(true);
    this.error.set(null);
    this.reportContent.set(null);
    this.recommendations.set(null);

    // Load history
    this.http.get<any>(`${this.aiBase}/student/${studentId}/history`).pipe(
      map((res: any) => res?.data ?? [])
    ).subscribe({
      next: (data: AIReportResult[]) => {
        this.reportHistory.set(Array.isArray(data) ? data : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  generateReport(studentId: number) {
    this.generating.set(true);
    this.error.set(null);
    this.http.get<any>(`${this.aiBase}/student/${studentId}/period/0`).pipe(
      map((res: any) => res?.data)
    ).subscribe({
      next: (data: AIReportResult) => {
        if (data) {
          this.reportContent.set(data.content);
          this.loadReports(studentId);
        }
        this.generating.set(false);
      },
      error: (err) => {
        this.error.set('حدث خطأ أثناء توليد التقرير');
        this.generating.set(false);
      }
    });
  }

  generateRecommendations(studentId: number) {
    this.generating.set(true);
    this.http.get<any>(`${this.aiBase}/recommendations/${studentId}`).pipe(
      map((res: any) => res?.data)
    ).subscribe({
      next: (data: AIReportResult) => {
        if (data) {
          this.recommendations.set(data.content);
        }
        this.generating.set(false);
      },
      error: () => this.generating.set(false)
    });
  }

  viewReport(id: number) {
    this.loading.set(true);
    this.http.get<any>(`${this.aiBase}/${id}`).pipe(
      map((res: any) => res?.data)
    ).subscribe({
      next: (data: AIReportResult) => {
        if (data) this.reportContent.set(data.content);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}

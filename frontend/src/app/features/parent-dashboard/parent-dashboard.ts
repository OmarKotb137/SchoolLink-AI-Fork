import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ParentDashboardChild, ParentDashboardService } from '../../core/services/parent-dashboard.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-parent-dashboard',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './parent-dashboard.html',
  styleUrl: './parent-dashboard.css'
})
export class ParentDashboard implements OnInit {
  private parentDashboardService = inject(ParentDashboardService);
  private authService = inject(AuthService);

  sidebarOpen = signal(false);
  displayUserName = computed(() => this.authService.user()?.fullName || 'ولي الأمر');

  children = signal<ParentDashboardChild[]>([]);
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);

  totalChildren = computed(() => this.children().length);
  activeChildren = computed(() => this.children().filter(child => child.isActive).length);
  inactiveChildren = computed(() => this.children().filter(child => !child.isActive).length);

  ngOnInit(): void {
    this.loadChildren();
  }

  loadChildren(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.parentDashboardService.getMyChildren().subscribe({
      next: (response) => {
        const data = response?.data ?? response ?? [];
        this.children.set(Array.isArray(data) ? data : []);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.children.set([]);
        this.errorMessage.set(err?.message || 'تعذر تحميل بيانات الأبناء');
        this.isLoading.set(false);
      }
    });
  }

  getRelationshipLabel(relationship: string): string {
    const labels: Record<string, string> = {
      Father: 'الأب',
      Mother: 'الأم',
      Guardian: 'ولي الأمر',
      Brother: 'أخ',
      Sister: 'أخت',
    };

    return labels[relationship] || relationship || 'غير محدد';
  }

  getChildClassLine(child: ParentDashboardChild): string {
    if (child.gradeLevelName && child.className) {
      return `${child.gradeLevelName} - ${child.className}`;
    }

    return child.gradeLevelName || child.className || 'غير مسجل بفصل حاليا';
  }
}

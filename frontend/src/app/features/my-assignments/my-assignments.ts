import { Component, signal, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-my-assignments',
  imports: [Sidebar, Topbar],
  templateUrl: './my-assignments.html',
  styleUrl: './my-assignments.css'
})
export class MyAssignments {
  private router = inject(Router);
  sidebarOpen = signal(false);
  activeFilter = signal<'all' | 'pending' | 'submitted' | 'late'>('all');

  assignments = signal([
    { id: 1, subject: 'الرياضيات', title: 'تمارين المعادلات التربيعية', deadline: '2026-06-10', remaining: '3 أيام', status: 'pending', score: null as number | null },
    { id: 2, subject: 'اللغة العربية', title: 'تحليل النص الشعري', deadline: '2026-06-08', remaining: 'يوم واحد', status: 'pending', score: null },
    { id: 3, subject: 'العلوم', title: 'تجربة الكهرباء', deadline: '2026-06-05', remaining: '—', status: 'submitted', score: 18 },
    { id: 4, subject: 'اللغة الإنجليزية', title: 'قواعد اللغة - الوحدة ٣', deadline: '2026-06-01', remaining: '—', status: 'late', score: 12 },
    { id: 5, subject: 'الرياضيات', title: 'مسائل الجبر', deadline: '2026-06-03', remaining: '—', status: 'submitted', score: 20 },
    { id: 6, subject: 'الدراسات الاجتماعية', title: 'الخرائط الجغرافية', deadline: '2026-06-12', remaining: '6 أيام', status: 'pending', score: null },
  ]);

  filteredAssignments = computed(() => {
    const f = this.activeFilter();
    const all = this.assignments();
    if (f === 'all') return all;
    return all.filter(a => a.status === f);
  });

  stats = computed(() => {
    const all = this.assignments();
    return {
      total: all.length,
      pending: all.filter(a => a.status === 'pending').length,
      submitted: all.filter(a => a.status === 'submitted').length,
      late: all.filter(a => a.status === 'late').length,
    };
  });

  startAssignment(id: number) {
    this.router.navigate(['/homework']);
  }

  setFilter(f: string) {
    this.activeFilter.set(f as any);
  }

  getStatusText(s: string): string {
    const m: Record<string, string> = { pending: 'قيد الانتظار', submitted: 'تم التسليم', late: 'متأخر' };
    return m[s] || s;
  }

  getStatusClass(s: string): string {
    const m: Record<string, string> = { pending: 'bg-secondary/10 text-secondary', submitted: 'bg-green-50 text-green-700', late: 'bg-error/10 text-error' };
    return m[s] || '';
  }
}

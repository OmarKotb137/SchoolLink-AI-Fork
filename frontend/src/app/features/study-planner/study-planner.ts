import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { AuthService } from '../../core/services/auth.service';
import { StudentService } from '../../core/services/student.service';
import { SubjectService } from '../../core/services/subject.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { StudyPlannerService } from '../../core/services/study-planner.service';
import { StudyPlanDto, StudyPlanItemDto, DAY_NAMES, PERIOD_NAMES, CreateStudyPlanItemRequest } from '../../core/models/study-plan.model';

@Component({
  selector: 'app-study-planner',
  imports: [Sidebar, Topbar, FormsModule],
  templateUrl: './study-planner.html',
  styleUrl: './study-planner.css',
})
export class StudyPlanner implements OnInit {
  private auth = inject(AuthService);
  private studentSvc = inject(StudentService);
  private subjectSvc = inject(SubjectService);
  private enrollmentSvc = inject(EnrollmentService);
  private academicYearSvc = inject(AcademicYearService);
  private plannerSvc = inject(StudyPlannerService);

  sidebarOpen = signal(false);
  enrollmentId = signal<number | null>(null);
  plan = signal<StudyPlanDto | null>(null);
  subjects = signal<{ id: number; name: string }[]>([]);
  sessions = signal<Map<string, StudyPlanItemDto[]>>(new Map());
  completed = signal<Set<number>>(new Set());
  loading = signal(false);
  weekLabel = signal('');
  errorMsg = signal('');
  hasPlan = signal(false);

  days = DAY_NAMES;
  periods = PERIOD_NAMES;

  restDayIndex = signal<number | null>(6);
  daysBeforeRest = computed(() => {
    const ri = this.restDayIndex();
    if (ri === null) return this.days;
    return this.days.slice(0, ri);
  });
  daysAfterRest = computed(() => {
    const ri = this.restDayIndex();
    if (ri === null) return [];
    return this.days.slice(ri + 1);
  });

  totalSessions = computed(() => {
    let c = 0;
    for (const arr of this.sessions().values()) c += arr.length;
    return c;
  });
  completedCount = computed(() => this.completed().size);
  completionPct = computed(() => {
    const t = this.totalSessions();
    return t ? Math.round(this.completedCount() / t * 100) : 0;
  });

  subjectColors = ['#2563eb', '#7c3aed', '#0891b2', '#059669', '#d97706', '#dc2626', '#6b7280', '#db2777', '#65a30d', '#0d9488'];

  private calcHours(item: StudyPlanItemDto): number {
    const sh = parseInt(item.startTime.split(':')[0], 10);
    const sm = parseInt(item.startTime.split(':')[1], 10);
    const eh = parseInt(item.endTime.split(':')[0], 10);
    const em = parseInt(item.endTime.split(':')[1], 10);
    return (eh + em / 60) - (sh + sm / 60);
  }

  totalHours = computed(() => {
    let t = 0;
    for (const arr of this.sessions().values())
      for (const item of arr) t += this.calcHours(item);
    return Math.round(t * 10) / 10;
  });

  subjectStats = computed(() => {
    const subs = this.subjects();
    const subMap = new Map<number, { id: number; name: string; hours: number }>();
    for (const s of subs) subMap.set(s.id, { id: s.id, name: s.name, hours: 0 });
    for (const arr of this.sessions().values())
      for (const item of arr) {
        const entry = subMap.get(item.subjectId);
        if (entry) entry.hours += this.calcHours(item);
      }
    for (const e of subMap.values()) e.hours = Math.round(e.hours * 10) / 10;
    const total = this.totalHours();
    const result = [...subMap.values()]
      .filter(s => s.hours > 0)
      .sort((a, b) => b.hours - a.hours)
      .map((s, i) => ({
        ...s,
        color: this.subjectColors[i % this.subjectColors.length],
        pct: total > 0 ? Math.round(s.hours / total * 100) : 0,
      }));
    return result;
  });

  donutGradient = computed(() => {
    const stats = this.subjectStats();
    if (!stats.length) return '';
    let total = 0;
    const segments = stats.map(s => {
      const start = total;
      total += s.pct;
      return { color: s.color, start, end: total };
    });
    return segments.map(s => `${s.color} ${s.start}% ${s.end}%`).join(', ');
  });

  hours = Array.from({ length: 12 }, (_, i) => {
    const v = i + 1;
    return v < 10 ? '0' + v : '' + v;
  });
  minutes = Array.from({ length: 12 }, (_, i) => {
    const v = i * 5;
    return v < 10 ? '0' + v : '' + v;
  });

  editStartHour = '08';
  editStartMin = '00';
  editStartPeriod = 'ص';
  editEndHour = '12';
  editEndMin = '00';
  editEndPeriod = 'م';

  timePopupVisible = signal(false);
  activeTimeSpan: HTMLElement | null = null;
  activeEditItem: StudyPlanItemDto | null = null;

  activePeriodIndex = signal(-1);
  activeDayIndex = signal(-1);
  addPopupVisible = signal(false);
  addError = signal('');
  addLoading = signal(false);
  newSession = { subjectId: 0, topic: '', notes: '' };
  addStartHour = '08';
  addStartMin = '00';
  addStartPeriod = 'ص';
  addEndHour = '12';
  addEndMin = '00';
  addEndPeriod = 'م';

  aiPreview = signal<StudyPlanDto | null>(null);
  aiPreviewVisible = signal(false);
  aiSelectingDays = signal(false);
  aiSelectedDays = signal<Set<number>>(new Set());
  backupPlanData = signal<{ startDate: string; endDate: string; items: CreateStudyPlanItemRequest[] } | null>(null);

  ngOnInit() {
    this.loadUserData();
  }

  private loadUserData() {
    const user = this.auth.user();
    if (!user) { this.errorMsg.set('لم يتم تسجيل الدخول'); return; }

    this.subjectSvc.getAll().subscribe({
      next: (res: any) => {
        const list = Array.isArray(res.data) ? res.data : Array.isArray(res) ? res : [];
        this.subjects.set(list.map((s: any) => ({ id: s.id, name: s.name })));
      }
    });

    this.academicYearSvc.getCurrent().subscribe({
      next: (res: any) => {
        const ayId = res?.data?.id ?? res?.id;
        if (!ayId) { this.errorMsg.set('لا توجد سنة دراسية نشطة'); return; }
        this.loadStudentWithEnrollment(user.userId, ayId);
      },
      error: () => { this.errorMsg.set('حدث خطأ في تحميل السنة الدراسية'); }
    });
  }

  private loadStudentWithEnrollment(userId: number, academicYearId: number) {
    this.studentSvc.getByUserId(userId).subscribe({
      next: (res: any) => {
        const student = res?.data ?? res;
        if (!student?.id) { this.errorMsg.set('لم يتم العثور على طالب'); return; }
        this.enrollmentSvc.getActiveByStudent(student.id, academicYearId).subscribe({
          next: (enr: any) => {
            const eid = enr?.data?.id ?? enr?.id;
            if (eid) {
              this.enrollmentId.set(eid);
              this.loadPlan(eid);
            } else {
              this.errorMsg.set('لا يوجد تسجيل نشط');
            }
          },
          error: () => { this.errorMsg.set('لا يوجد تسجيل نشط'); }
        });
      },
      error: () => { this.errorMsg.set('لم يتم العثور على طالب'); }
    });
  }

  private loadPlan(enrollmentId: number) {
    this.loading.set(true);
    this.plannerSvc.getActivePlan(enrollmentId).subscribe({
      next: (res: any) => {
        const p = res?.data as StudyPlanDto;
        if (p) {
          this.plan.set(p);
          this.hasPlan.set(true);
          this.restDayIndex.set(p.restDay ?? 6);
          this.buildSessions(p);
          const start = new Date(p.startDate);
          const end = new Date(p.endDate);
          this.weekLabel.set(`${start.toLocaleDateString('ar-SA')} - ${end.toLocaleDateString('ar-SA')}`);
        }
        this.loading.set(false);
      },
      error: () => {
        this.plan.set(null);
        this.hasPlan.set(false);
        this.sessions.set(new Map());
        this.completed.set(new Set());
        this.loading.set(false);
        this.errorMsg.set('لا توجد خطة نشطة - اضغط "اقتراح AI" لإنشاء خطة');
      }
    });
  }

  private periodForTime(time: string): number {
    const h = parseInt(time.split(':')[0], 10);
    if (h >= 8 && h < 12) return 0;
    if (h >= 12 && h < 16) return 1;
    if (h >= 16 && h < 20) return 2;
    return 3;
  }

  private buildSessions(plan: StudyPlanDto) {
    const map = new Map<string, StudyPlanItemDto[]>();
    const comp = new Set<number>();
    for (const item of plan.items) {
      const gridCol = item.dayOfWeek;
      if (gridCol < 0 || gridCol > 6) continue;
      const periodIndex = this.periodForTime(item.startTime);
      const key = `${periodIndex}-${gridCol}`;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(item);
      if (item.isCompleted) comp.add(item.id);
    }
    this.sessions.set(map);
    this.completed.set(comp);
  }

  getSessions(pi: number, di: number) {
    const key = pi + '-' + di;
    return { key, items: this.sessions().get(key) ?? [] };
  }

  toggleComplete(item: StudyPlanItemDto, event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    const eid = this.enrollmentId();
    if (!eid) return;
    if (checked) {
      this.plannerSvc.markComplete(item.id, eid).subscribe({ error: () => this.loadPlan(eid) });
    } else {
      this.plannerSvc.markIncomplete(item.id, eid).subscribe({ error: () => this.loadPlan(eid) });
    }
    this.completed.update(s => {
      const next = new Set(s);
      if (checked) next.add(item.id);
      else next.delete(item.id);
      return next;
    });
  }

  deleteSession(itemId: number, key: string) {
    const eid = this.enrollmentId();
    if (!eid) return;
    this.plannerSvc.deleteSession(itemId, eid).subscribe({
      next: () => {
        this.sessions.update(m => {
          const arr = m.get(key);
          if (arr) {
            const filtered = arr.filter(i => i.id !== itemId);
            if (filtered.length) m.set(key, filtered);
            else m.delete(key);
          }
          return new Map(m);
        });
        this.completed.update(s => { const n = new Set(s); n.delete(itemId); return n; });
      },
      error: () => this.errorMsg.set('فشل حذف الجلسة')
    });
  }

  private to24h(hour: string, minute: string, period: string): string {
    let h = parseInt(hour, 10);
    if (period === 'م' && h !== 12) h += 12;
    if (period === 'ص' && h === 12) h = 0;
    return `${h < 10 ? '0' + h : h}:${minute}:00`;
  }

  to12h(time: string): string {
    const parts = time.split(':');
    if (parts.length < 2) return time;
    let h = parseInt(parts[0], 10);
    const m = parts[1];
    const ampm = h >= 12 ? 'م' : 'ص';
    if (h > 12) h -= 12;
    if (h === 0) h = 12;
    return `${h}:${m} ${ampm}`;
  }

  generateWithAI() {
    const eid = this.enrollmentId();
    if (!eid) { this.errorMsg.set('لا يوجد تسجيل نشط'); return; }

    const currentPlan = this.plan();
    if (currentPlan) {
      this.backupPlanData.set({
        startDate: currentPlan.startDate,
        endDate: currentPlan.endDate,
        items: currentPlan.items.map(i => {
          let st = i.startTime, et = i.endTime;
          if (st >= et) { const t = st; st = et; et = t; }
          return {
            subjectId: i.subjectId,
            dayOfWeek: i.dayOfWeek,
            startTime: st,
            endTime: et,
            topic: i.topic,
            notes: i.notes,
          };
        }),
      });
    } else {
      this.backupPlanData.set(null);
    }

    this.loading.set(true);
    this.errorMsg.set('');
    const today = new Date();
    const end = new Date(today);
    end.setDate(end.getDate() + 7);
    const fmt = (d: Date) => d.toISOString().slice(0, 10);
    this.plannerSvc.generatePlan({
      enrollmentId: eid,
      startDate: fmt(today),
      endDate: fmt(end),
      aiPromptSummary: 'خطة أسبوعية مقترحة'
    }).subscribe({
      next: (res: any) => {
        const p = res?.data as StudyPlanDto;
        if (p) {
          this.aiPreview.set(p);
          this.aiPreviewVisible.set(true);
          this.aiSelectingDays.set(false);
          this.aiSelectedDays.set(new Set());
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMsg.set('فشل إنشاء الخطة - حاول مرة أخرى');
      }
    });
  }

  applyAiPlan() {
    const p = this.aiPreview();
    if (!p) return;
    this.plan.set(p);
    this.hasPlan.set(true);
    this.buildSessions(p);
    const start = new Date(p.startDate);
    const end = new Date(p.endDate);
    this.weekLabel.set(`${start.toLocaleDateString('ar-SA')} - ${end.toLocaleDateString('ar-SA')}`);
    this.dismissAiPreview();
    this.errorMsg.set('');
  }

  restoreMyPlan() {
    const eid = this.enrollmentId();
    const backup = this.backupPlanData();
    if (eid && backup) {
      this.loading.set(true);
      const fixedItems = backup.items.map(i => ({
        ...i,
        startTime: i.startTime >= i.endTime ? i.endTime : i.startTime,
        endTime: i.startTime >= i.endTime ? i.startTime : i.endTime,
      }));
      this.plannerSvc.createManualPlan({
        enrollmentId: eid,
        startDate: backup.startDate,
        endDate: backup.endDate,
        restDay: this.restDayIndex(),
        items: fixedItems,
      }).subscribe({
        next: () => {
          this.loadPlan(eid);
          this.dismissAiPreview();
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.errorMsg.set('فشل استعادة الخطة القديمة');
          this.dismissAiPreview();
        }
      });
    } else {
      if (eid) this.loadPlan(eid);
      this.dismissAiPreview();
    }
  }

  showDaySelection() {
    this.aiSelectingDays.set(true);
    this.aiSelectedDays.set(new Set());
  }

  toggleAiDay(gridCol: number) {
    this.aiSelectedDays.update(s => {
      const next = new Set(s);
      if (next.has(gridCol)) next.delete(gridCol);
      else next.add(gridCol);
      return next;
    });
  }

  confirmSelectedDays() {
    const eid = this.enrollmentId();
    const backup = this.backupPlanData();
    const aiP = this.aiPreview();
    if (!eid || !backup || !aiP) return;

    const selectedAiItems = aiP.items
      .filter(i => this.aiSelectedDays().has(i.dayOfWeek))
      .map(i => ({
        subjectId: i.subjectId,
        dayOfWeek: i.dayOfWeek,
        startTime: i.startTime,
        endTime: i.endTime,
        topic: i.topic,
        notes: i.notes,
      }));

    const nonSelectedBackupItems = backup.items.filter(i => !this.aiSelectedDays().has(i.dayOfWeek));

    const merged = [...nonSelectedBackupItems, ...selectedAiItems];

    this.loading.set(true);
    this.plannerSvc.createManualPlan({
      enrollmentId: eid,
      startDate: backup.startDate,
      endDate: backup.endDate,
      restDay: this.restDayIndex(),
      items: merged,
    }).subscribe({
      next: () => {
        this.loadPlan(eid);
        this.dismissAiPreview();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMsg.set('فشل تطبيق الأيام المحددة');
      }
    });
  }

  aiGroupedSessions(): { dayName: string; dayIndex: number; items: StudyPlanItemDto[] }[] {
    const p = this.aiPreview();
    if (!p) return [];
    const groups: { dayName: string; dayIndex: number; items: StudyPlanItemDto[] }[] = [];
    for (const item of p.items) {
      const gridCol = item.dayOfWeek;
      const dayName = this.days[gridCol];
      let group = groups.find(g => g.dayIndex === gridCol);
      if (!group) {
        group = { dayName, dayIndex: gridCol, items: [] };
        groups.push(group);
      }
      group.items.push(item);
    }
    groups.sort((a, b) => a.dayIndex - b.dayIndex);
    return groups;
  }

  dismissAiPreview() {
    this.aiPreviewVisible.set(false);
    this.aiPreview.set(null);
    this.aiSelectingDays.set(false);
    this.aiSelectedDays.set(new Set());
  }

  openAddSession(pi: number, di: number) {
    this.activePeriodIndex.set(pi);
    this.activeDayIndex.set(di);
    this.newSession = { subjectId: 0, topic: '', notes: '' };
    const period = this.periods[pi];
    this.addStartHour = period.start.split(':')[0];
    this.addStartMin = period.start.split(':')[1] || '00';
    this.addStartPeriod = parseInt(this.addStartHour, 10) >= 12 ? 'م' : 'ص';
    this.addEndHour = period.end.split(':')[0];
    this.addEndMin = period.end.split(':')[1] || '00';
    this.addEndPeriod = parseInt(this.addEndHour, 10) >= 12 ? 'م' : 'ص';
    this.addError.set('');
    this.addPopupVisible.set(true);
  }

  onSubjectChange(val: string) {
    this.newSession.subjectId = parseInt(val, 10) || 0;
    this.addError.set('');
  }

  onTopicChange(val: string) {
    this.newSession.topic = val;
  }

  saveNewSession() {
    const eid = this.enrollmentId();
    if (!eid) { this.addError.set('لا يوجد تسجيل نشط'); return; }
    if (!this.newSession.subjectId) { this.addError.set('اختر مادة'); return; }

    const gridCol = this.activeDayIndex();

    this.addLoading.set(true);
    this.addError.set('');

    const newItem = {
      subjectId: this.newSession.subjectId,
      dayOfWeek: gridCol,
      startTime: this.to24h(this.addStartHour, this.addStartMin, this.addStartPeriod),
      endTime: this.to24h(this.addEndHour, this.addEndMin, this.addEndPeriod),
      topic: this.newSession.topic || undefined,
      notes: this.newSession.notes || undefined,
    };

    const plan = this.plan();
    if (plan) {
      const existingItems = plan.items.map(i => ({
        subjectId: i.subjectId,
        dayOfWeek: i.dayOfWeek,
        startTime: i.startTime,
        endTime: i.endTime,
        topic: i.topic,
        notes: i.notes,
      }));
      existingItems.push(newItem);
      this.plannerSvc.createManualPlan({
        enrollmentId: eid,
        startDate: plan.startDate,
        endDate: plan.endDate,
        restDay: this.restDayIndex(),
        items: existingItems,
      }).subscribe({
        next: () => {
          this.addLoading.set(false);
          this.addPopupVisible.set(false);
          this.loadPlan(eid);
        },
        error: (err) => {
          this.addLoading.set(false);
          this.addError.set('فشل إضافة الجلسة - ' + (err?.error?.message || 'خطأ في الاتصال'));
        }
      });
    } else {
      const today = new Date();
      const end = new Date(today);
      end.setDate(end.getDate() + 7);
      const fmt = (d: Date) => d.toISOString().slice(0, 10);
      this.plannerSvc.createManualPlan({
        enrollmentId: eid,
        startDate: fmt(today),
        endDate: fmt(end),
        restDay: this.restDayIndex(),
        items: [newItem],
      }).subscribe({
        next: () => {
          this.addLoading.set(false);
          this.addPopupVisible.set(false);
          this.loadPlan(eid);
        },
        error: (err) => {
          this.addLoading.set(false);
          this.addError.set('فشل إضافة الجلسة - ' + (err?.error?.message || 'خطأ في الاتصال'));
        }
      });
    }
  }

  closeAddPopup() {
    this.addPopupVisible.set(false);
    this.addError.set('');
  }

  openTimePopup(item: StudyPlanItemDto, el: HTMLElement | null) {
    this.activeEditItem = item;
    this.activeTimeSpan = el;
    if (el) {
      const txt = el.textContent?.trim().replace(/\s+/g, ' ') ?? '';
      const parts = txt.split('-');
      if (parts.length === 2) {
        const start = parts[0].trim().split(' ');
        const end = parts[1].trim().split(' ');
        if (start.length === 2) {
          const st = start[0].split(':');
          if (st.length === 2) { this.editStartHour = st[0]; this.editStartMin = st[1]; }
          this.editStartPeriod = start[1];
        }
        if (end.length === 2) {
          const et = end[0].split(':');
          if (et.length === 2) { this.editEndHour = et[0]; this.editEndMin = et[1]; }
          this.editEndPeriod = end[1];
        }
      }
    }
    this.timePopupVisible.set(true);
  }

  closeTimePopup() {
    this.timePopupVisible.set(false);
    this.activeTimeSpan = null;
    this.activeEditItem = null;
  }

  saveTimePopup() {
    const eid = this.enrollmentId();
    const item = this.activeEditItem;
    if (item && eid) {
      const newStart = this.to24h(this.editStartHour, this.editStartMin, this.editStartPeriod);
      const newEnd = this.to24h(this.editEndHour, this.editEndMin, this.editEndPeriod);
      this.plannerSvc.updateSession(item.id, {
        id: item.id,
        studyPlanId: item.studyPlanId,
        startTime: newStart,
        endTime: newEnd,
      }).subscribe({
        next: () => this.loadPlan(eid),
        error: () => this.errorMsg.set('فشل تحديث الوقت')
      });
    }
    if (this.activeTimeSpan) {
      this.activeTimeSpan.textContent =
        this.editStartHour + ':' + this.editStartMin + ' ' + this.editStartPeriod + ' - ' +
        this.editEndHour + ':' + this.editEndMin + ' ' + this.editEndPeriod;
    }
    this.closeTimePopup();
  }

  onRestDayChange(value: number | null) {
    this.restDayIndex.set(value);
    const p = this.plan();
    if (p) {
      this.plannerSvc.updateRestDay(p.id, value).subscribe({
        error: () => this.errorMsg.set('فشل حفظ يوم الراحة')
      });
    }
  }

  canAddSession(gridCol: number): boolean {
    const ri = this.restDayIndex();
    if (ri === null) return true;
    return gridCol !== ri;
  }

  dismissError() {
    this.errorMsg.set('');
  }
}
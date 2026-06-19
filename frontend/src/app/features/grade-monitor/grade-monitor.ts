import { Component, signal, computed, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, forkJoin, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { GradeMonitorService, Template, ClassItem, Student, SchoolProfile, EvaluationPeriod, Criteria } from './grade-monitor.service';
import { GradeLevelService, GradeLevel } from '../../core/services/grade-level.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { RoleService } from '../../shared/role.service';
import { WordGeneratorService } from './word-generator.service';

@Component({
  selector: 'app-grade-monitor',
  imports: [Sidebar, FormsModule],
  templateUrl: './grade-monitor.html',
  styleUrl: './grade-monitor.css',
})
export class GradeMonitor implements OnInit {
  private api = inject(GradeMonitorService);
  private gradeLevelService = inject(GradeLevelService);
  private wg = inject(WordGeneratorService);
  private cdr = inject(ChangeDetectorRef);
  private academicYearService = inject(AcademicYearService);
  private roleService = inject(RoleService);

  sidebarOpen = signal(false);
  activeSection = signal<'dash' | 'tmpl' | 'cls' | 'entry' | 'exp'>('dash');

  // ══════════════════════════════════════
  //  SIGNALS
  // ══════════════════════════════════════
  templates = signal<Template[]>([]);
  classes = signal<ClassItem[]>([]);          // linked classes (from ClassTemplateLinks)
  apiClasses = signal<any[]>([]);             // real classes from API (for dropdown)
  grades = signal<GradeLevel[]>([]);          // grade levels for filtering
  schoolProfile = signal<SchoolProfile | null>(null);
  periods = signal<EvaluationPeriod[]>([]);
  subjects = signal<{ id: number; name: string }[]>([]);
  teacherSubjects = signal<{ id: number; name: string }[]>([]);
  studentCount = signal(0);
  academicYearId = signal<number>(1);
  selectedGradeLevelId = signal<number>(1);
  editTmplId = signal<number | null>(null);
  loadingTemplates = signal(false);
  loadingClasses = signal(false);
  loadingApiClasses = signal(false);
  loadingGradesState = signal(false);
  isTeacherMode = signal(false);
  editClsId = signal<number | null>(null);
  private STORAGE_KEY = 'grade_monitor_classes';

  entryClassId = signal<number | null>(null);
  entryType = signal<'grades' | 'absence' | 'exams' | 'final'>('grades');
  entryTerm = signal<number>(1); // 1 = first semester, 2 = second semester
  entryWeek = signal<number>(1);
  entryTemplateId = signal<number | null>(null); // لاختيار قالب/مادة مختلف
  enrollmentMap = signal<Record<number, number>>({}); // studentId → enrollmentId
  existingEvalMap = signal<Record<string, number>>({}); // key: enrollmentId_evalItemId → evalId

  // Local objects for two-way binding input values
  localGrades: Record<string, any> = {};
  localAbsences: Record<string, boolean> = {};
  localExams: Record<string, any> = {};
  localFinals: Record<string, any> = {};
  finalCompleteStatus = signal<Record<string, boolean>>({});
  finalMaxValues = signal<Record<string, number>>({});
  private finalTouched: Set<string> = new Set();
  private absenceSnapshot: Record<string, boolean> = {};

  // ══ Loading indicator for week data ══
  loadingWeek = signal(false);
  dataReady = signal(false);
  pendingReload = signal(false);
  // إلغاء الطلبات القديمة عند استدعاء جديد
  private cancelLoad$ = new Subject<void>();

  currentPeriodId = computed(() => {
    const weekNum = Number(this.entryWeek());
    const term = this.entryTerm();
    const p = this.periods().find(p =>
      p.periodType === 'Weekly' &&
      p.orderNum === weekNum &&
      (term == null || p.semesterNumber == null || p.semesterNumber === term)
    );
    return p?.id ?? null;
  });

  exportClassId = signal<number | null>(null);
  exportType = signal<'weekly' | 'attendance' | 'summary'>('weekly');
  exporting = signal(false);

  // Loading / status
  saving = signal(false);
  snackbar = signal('');
  quickEntryClassId = signal<number | null>(null);

  entryWeeksForQuick = computed(() => {
    const cid = this.quickEntryClassId();
    if (!cid) return [];
    const cls = this.visibleClasses().find(c => c.id === cid);
    if (!cls) return [];
    const tmpl = this.templates().find(t => t.id === cls.template_id);
    if (!tmpl) return [];
    return Array.from({ length: tmpl.weeks }, (_, i) => i + 1);
  });

  weekLabelForQuick(wn: number): string {
    return 'الأسبوع ' + wn;
  }

  // ══════════════════════════════════════
  //  INIT
  // ══════════════════════════════════════
  ngOnInit() {
    // Auto-detect teacher role → show only their classes
    const role = this.roleService.currentRole();
    if (role === 'teacher') {
      this.isTeacherMode.set(true);
    }

    this.loadFromLocalStorage();
    this.loadGrades();
    this.loadApiClasses();
    this.loadTeacherSubjects();

    this.academicYearService.getCurrentTerm().subscribe({
      next: (res) => {
        if (res?.data != null) {
          // Map enum string to number (FirstSemester=1, SecondSemester=2, Final=3)
          const termMap: Record<string, number> = { FirstSemester: 1, SecondSemester: 2, Final: 3 };
          this.entryTerm.set(termMap[res.data as string] ?? 1);
        }
      }
    });

    // Load current academic year first, then use it
    this.api.getCurrentAcademicYear().subscribe({
      next: (res) => {
        const yearId = res?.isSuccess ? res.data?.id : (res?.id ?? 1);
        this.academicYearId.set(yearId);
        this.finishInit(yearId);
      },
      error: () => this.finishInit(1),
    });
  }

  private finishInit(academicYearId: number) {
    Promise.all([
      new Promise<void>(resolve => {
        this.api.getSchoolProfile().subscribe({
          next: (res) => { if (res?.isSuccess) this.schoolProfile.set(res.data); else if (res) this.schoolProfile.set(res); resolve(); },
          error: () => resolve(),
        });
      }),
      new Promise<void>(resolve => {
        this.api.getPeriodsByAcademicYear(academicYearId).subscribe(res => {
          if (res.isSuccess) this.periods.set(res.data || []);
          resolve();
        });
      }),
      new Promise<void>(resolve => {
        this.api.getSubjects().subscribe(res => {
          if (res.isSuccess) this.subjects.set(res.data || []);
          resolve();
        });
      }),
      this.loadLinksFromApi(),
      this.loadTemplatesAsync(academicYearId),
      new Promise<void>(resolve => {
        this.api.getStudents().subscribe({
          next: (res) => { this.studentCount.set(Array.isArray(res) ? res.length : (res.data?.length || 0)); resolve(); },
          error: () => resolve(),
        });
      }),
    ]).then(() => {
      this.dataReady.set(true);
      if (this.pendingReload()) {
        this.pendingReload.set(false);
        this.loadEvaluations();
      }
    });
  }

  // ══════════════════════════════════════
  //  TEMPLATES
  // ══════════════════════════════════════
  tName = signal('');
  tStage = signal('الصف الأول الإعدادي');
  tSubjectId = signal<number | null>(null);
  tWeeks = signal(12);
  tStart = signal('');
  tSchool = signal('');
  tDir = signal('');
  tAdm = signal('');
  tCriteria = signal<Criteria[]>([
    { id: 'default_1', name: 'الأداء المنزلي', max: 10, autoCalcType: 0, absenceMax: undefined },
    { id: 'default_2', name: 'التقييم الأسبوعي', max: 20, autoCalcType: 0, absenceMax: undefined },
    { id: 'default_3', name: 'السلوك والمواظبة', max: 10, autoCalcType: 1, absenceMax: 10 },
  ]);
  tSumCols = signal<{ id: string; name: string; max: number }[]>([
    { id: 'feb_avg', name: 'متوسط شهر فبراير', max: 40 },
    { id: 'mar_avg', name: 'متوسط شهر مارس', max: 40 },
    { id: 'apr_avg', name: 'متوسط شهر أبريل', max: 40 },
    { id: 'three_month_avg', name: 'متوسط الثلاثة شهور', max: 40 },
    { id: 'exam1', name: 'الاختبار الشهري الأول', max: 15 },
    { id: 'exam2', name: 'الاختبار الشهري الثاني', max: 15 },
    { id: 'written_total', name: 'مجموع الأعمال التحريرية', max: 70 },
    { id: 'end_year', name: 'درجة آخر العام', max: 30 },
    { id: 'final_total', name: 'المجموع النهائي', max: 100 },
  ]);
  tAbsentDays = signal<string[]>(['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس']);
  tTerm = signal<number>(1); // 1 = الترم الأول, 2 = الترم الثاني
  tmplModalOpen = signal(false);

  private async loadTemplatesAsync(academicYearId?: number) {
    this.loadingTemplates.set(true);
    const ayId = academicYearId ?? this.academicYearId();
    const gradeLevelId = this.selectedGradeLevelId();
    const res = await firstValueFrom(this.api.getTemplatesByGradeLevel(gradeLevelId, ayId));
    if (!res.isSuccess) { this.loadingTemplates.set(false); return; }
    const rawList = res.data as any[];
    if (!rawList?.length) {
      const stored = this.loadRawFromLocalStorage();
      if (stored?.templates?.length) {
        this.templates.set([]);
        this.saveToLocalStorage();
      }
      return;
    }
    const apiIds = new Set(rawList.map((t: any) => t.id));
    const localTemplates = this.templates().filter(t => !t.apiId || !apiIds.has(t.apiId));
    this.templates.set(localTemplates);
    const results = await Promise.all(rawList.map(t =>
      firstValueFrom(this.api.getItemsByTemplate(t.id))
    ));
    for (let i = 0; i < rawList.length; i++) {
      const t = rawList[i];
      const itemRes = results[i];
      const items = itemRes.isSuccess ? (itemRes.data as any[]) || [] : [];
      const criteria = items
        .filter((i: any) => i.isVisible !== false)
        .sort((a: any, b: any) => a.displayOrder - b.displayOrder)
        .map((i: any) => ({ id: 'c' + i.id, name: i.name, max: i.maxScore, autoCalcType: i.autoCalcType ?? 0, absenceMax: i.absenceMaxScore ?? undefined }));
      const weeklyMax = criteria.reduce((a: number, c: any) => a + c.max, 0);
      this.templates.update(list => [...list, this.toFrontendTemplate(t, criteria, weeklyMax)]);
    }
    this.saveToLocalStorage();
    this.loadingTemplates.set(false);
  }

  computeMonthGroups(startDateStr: string, totalWeeks: number) {
    if (!startDateStr) {
      return [
        { name: 'شهر 1', weeks: Array.from({ length: Math.min(4, totalWeeks) }, (_, i) => i + 1) },
        { name: 'شهر 2', weeks: Array.from({ length: Math.min(4, totalWeeks - 4) }, (_, i) => i + 5) },
        { name: 'شهر 3', weeks: Array.from({ length: Math.max(0, totalWeeks - 8) }, (_, i) => i + 9) },
      ].filter(g => g.weeks.length > 0);
    }

    const startDate = new Date(startDateStr);
    const arabicMonths = [
      "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
      "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
    ];

    const groups: { name: string; weeks: number[] }[] = [];
    const date = new Date(startDate);

    for (let w = 1; w <= totalWeeks; w++) {
      const monthName = arabicMonths[date.getMonth()];
      let g = groups.find(x => x.name === monthName);
      if (!g) {
        g = { name: monthName, weeks: [] };
        groups.push(g);
      }
      g.weeks.push(w);
      date.setDate(date.getDate() + 7);
    }

    return groups;
  }

  onTmplConfigChange() {
    this.updateSuggestedCols();
  }

  onTmplTermChange(term: number) {
    this.tTerm.set(term);
    this.academicYearService.getCurrent().subscribe({
        next: (res) => {
          if (res?.isSuccess && res.data) {
            const year = res.data;
            const startStr = term === 1 ? year.firstSemesterStartDate : year.secondSemesterStartDate;
            const endStr = term === 1 ? year.firstSemesterEndDate : year.secondSemesterEndDate;
            if (startStr) {
              this.tStart.set(startStr);
            }
            if (startStr && endStr) {
              const start = new Date(startStr);
              const end = new Date(endStr);
              const diffDays = Math.round((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
              const weeks = Math.max(1, Math.round((diffDays + 6) / 7));
              this.tWeeks.set(weeks);
            }
            this.updateSuggestedCols();
          }
        }
      });
  }

  private updateSuggestedCols() {
    const startDateStr = this.tStart();
    const weeksCount = Number(this.tWeeks()) || 12;
    if (!startDateStr) return;

    const mGroups = this.computeMonthGroups(startDateStr, weeksCount);
    if (!mGroups.length) return;

    const month1Name = mGroups[0]?.name || 'الأول';
    const month2Name = mGroups[1]?.name || 'الثاني';
    const month3Name = mGroups[2]?.name || 'الثالث';

    const suggestedCols = [
      { id: 'm1_avg', name: `متوسط شهر ${month1Name}`, max: 40 },
      { id: 'm2_avg', name: `متوسط شهر ${month2Name}`, max: 40 },
      { id: 'm3_avg', name: `متوسط شهر ${month3Name}`, max: 40 },
      { id: 'three_month_avg', name: 'متوسط الثلاثة شهور', max: 40 },
      { id: 'exam1', name: `امتحان شهر ${month1Name}`, max: 15 },
      { id: 'exam2', name: `امتحان شهر ${month2Name}`, max: 15 },
      { id: 'written_total', name: 'مجموع الأعمال التحريرية', max: 70 },
      { id: 'end_year', name: 'درجة آخر العام', max: 30 },
      { id: 'final_total', name: 'المجموع النهائي', max: 100 },
    ];

    this.tSumCols.set(suggestedCols);
  }

  toggleSchoolDay(day: string) {
    this.tAbsentDays.update(days =>
      days.includes(day) ? days.filter(d => d !== day) : [...days, day]
    );
  }

  private toFrontendTemplate(t: any, criteria: { id: string; name: string; max: number }[], weeklyMax: number): Template {
    const p = this.schoolProfile();
    const weekNames = ['الأول', 'الثاني', 'الثالث', 'الرابع', 'الخامس', 'السادس', 'السابع',
      'الثامن', 'التاسع', 'العاشر', 'الحادي عشر', 'الثاني عشر'];
    // Filter periods by the template's term if specified
    const term = t.term;
    let weeks = this.periods().filter((p: any) => p.periodType === 'Weekly');
    if (term != null) {
      weeks = weeks.filter(w => w.semesterNumber == null || w.semesterNumber === this.normalizeTerm(term));
    }
    // If term is specified, re-number weeks sequentially (1,2,3…) instead of using DB names (19,20,21…)
    const wNames = weeks.map((w: any, i: number) =>
      term != null ? ('الأسبوع ' + (i + 1)) : (w.name || weekNames[i] || '')
    );
    const wDates = weeks.map((w: any) => {
      if (!w.startDate) return '';
      const d = new Date(w.startDate);
      return d.getDate() + '/' + (d.getMonth() + 1) + '/' + d.getFullYear();
    });
    const weekCount = t.weeks || weeks.length || 12;
    const startDateStr = t.start_date || weeks[0]?.startDate || '';
    const mGroups = this.computeMonthGroups(startDateStr, weekCount);

    return {
      id: t.id,
      apiId: t.id,
      name: t.name,
      subjectName: t.subjectName,
      stage: t.gradeLevelName || '',
      subjectId: t.subjectId,
      weeks: weekCount,
      start_date: startDateStr,
      school: p?.schoolName || '',
      directorate: p?.directorate || '',
      administration: p?.educationalAdministration || '',
      criteria,
      summary_columns: t.summary_columns?.length ? t.summary_columns : [
        { id: 'feb_avg', name: 'متوسط شهر فبراير', max: 40 },
        { id: 'mar_avg', name: 'متوسط شهر مارس', max: 40 },
        { id: 'apr_avg', name: 'متوسط شهر أبريل', max: 40 },
        { id: 'three_month_avg', name: 'متوسط الثلاثة شهور', max: 40 },
        { id: 'exam1', name: 'الاختبار الشهري الأول', max: 15 },
        { id: 'exam2', name: 'الاختبار الشهري الثاني', max: 15 },
        { id: 'written_total', name: 'مجموع الأعمال التحريرية', max: 70 },
        { id: 'end_year', name: 'درجة آخر العام', max: 30 },
        { id: 'final_total', name: 'المجموع النهائي', max: 100 },
      ],
      week_names: wNames,
      week_dates: wDates,
      absent_days: t.absent_days?.length ? t.absent_days : ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس'],
      weekly_max: weeklyMax,
      month_groups: mGroups,
      term: t.term,
    };
  }

  openTmplForm(t?: Template) {
    const profile = this.schoolProfile();
    this.editTmplId.set(t ? t.id : null);
    this.tName.set(t?.name || '');
    this.tStage.set(t?.stage || 'الصف الأول الإعدادي');
    this.tSubjectId.set(t?.subjectId ?? null);
    this.tWeeks.set(t?.weeks || 12);
    this.tStart.set(t?.start_date || '');
    this.tSchool.set(t?.school || profile?.schoolName || '');
    this.tDir.set(t?.directorate || profile?.directorate || '');
    this.tAdm.set(t?.administration || profile?.educationalAdministration || '');
    this.tAbsentDays.set(t?.absent_days?.length ? t.absent_days : ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس']);
    this.tTerm.set(t?.term ?? 1);

    const criteria = t?.criteria?.length ? t.criteria : [
      { id: 'default_1', name: 'الأداء المنزلي', max: 10, autoCalcType: 0, absenceMax: undefined },
      { id: 'default_2', name: 'التقييم الأسبوعي', max: 20, autoCalcType: 0, absenceMax: undefined },
      { id: 'default_3', name: 'السلوك والمواظبة', max: 10, autoCalcType: 0, absenceMax: undefined },
    ];
    this.tCriteria.set(criteria.map(c => ({ ...c })));

    const sumCols = t?.summary_columns?.length ? t.summary_columns : [
      { id: 'feb_avg', name: 'متوسط شهر فبراير', max: 40 },
      { id: 'mar_avg', name: 'متوسط شهر مارس', max: 40 },
      { id: 'apr_avg', name: 'متوسط شهر أبريل', max: 40 },
      { id: 'three_month_avg', name: 'متوسط الثلاثة شهور', max: 40 },
      { id: 'exam1', name: 'الاختبار الشهري الأول', max: 15 },
      { id: 'exam2', name: 'الاختبار الشهري الثاني', max: 15 },
      { id: 'written_total', name: 'مجموع الأعمال التحريرية', max: 70 },
      { id: 'end_year', name: 'درجة آخر العام', max: 30 },
      { id: 'final_total', name: 'المجموع النهائي', max: 100 },
    ];
    this.tSumCols.set(sumCols.map(c => ({ ...c })));
    this.tmplModalOpen.set(true);
  }

  saveTmpl() {
    if (!this.tName().trim()) return;
    this.saving.set(true);

    const profile = this.schoolProfile();
    const nextId = this.templates().length ? Math.max(...this.templates().map(t => t.id)) + 1 : 1;
    const weekNames = ['الأول', 'الثاني', 'الثالث', 'الرابع', 'الخامس', 'السادس', 'السابع',
      'الثامن', 'التاسع', 'العاشر', 'الحادي عشر', 'الثاني عشر', 'الثالث عشر', 'الرابع عشر', 'الخامس عشر', 'السادس عشر'];
    const weekDates: string[] = [];
    if (this.tStart()) {
      let d = new Date(this.tStart());
      for (let i = 0; i < this.tWeeks(); i++) {
        weekDates.push(d.getDate() + '/' + (d.getMonth() + 1) + '/' + d.getFullYear());
        d.setDate(d.getDate() + 7);
      }
    }
    const monthGroups = this.computeMonthGroups(this.tStart(), this.tWeeks());
    const data: Template = {
      id: this.editTmplId() || nextId,
      name: this.tName().trim(),
      stage: this.tStage(),
      subjectId: this.tSubjectId() ?? undefined,
      weeks: this.tWeeks(),
      start_date: this.tStart(),
      school: this.tSchool() || profile?.schoolName || '',
      directorate: this.tDir() || profile?.directorate || '',
      administration: this.tAdm() || profile?.educationalAdministration || '',
      criteria: this.tCriteria(),
      summary_columns: this.tSumCols(),
      week_names: weekNames.slice(0, this.tWeeks()),
      week_dates: weekDates,
      absent_days: this.tAbsentDays(),
      weekly_max: this.tCriteria().reduce((a, c) => a + c.max, 0),
      month_groups: monthGroups,
      term: this.tTerm(),
    };

    const existing = this.editTmplId() ? this.templates().find(t => t.id === this.editTmplId()) : null;
    const apiId = existing?.apiId;

    const saveLocal = () => {
      if (existing) {
        this.templates.update(list => list.map(t => t.id === existing.id ? data : t));
      } else {
        this.templates.update(list => [...list, data]);
      }
      this.saveToLocalStorage();
      this.tmplModalOpen.set(false);
      this.saving.set(false);
      this.showSnackbar(this.editTmplId() ? 'تم تحديث القالب' : 'تم إنشاء القالب');
    };

    const saveItems = (tmplApiId: number) => {
      data.apiId = tmplApiId;
      if (this.editTmplId()) data.id = tmplApiId;

      const processItems = () => {
        const requests = data.criteria.map((c, idx) => {
          const body: any = {
            templateId: tmplApiId,
            name: c.name,
            maxScore: c.max,
            weight: 1,
            itemType: 1,
            autoCalcType: c.autoCalcType ?? 0,
            displayOrder: idx + 1,
            isVisible: true
          };
          if (c.autoCalcType === 1) body.absenceMaxScore = c.absenceMax ?? c.max;
          const existingItemId = c.id.startsWith('c') ? parseInt(c.id.substring(1)) : null;
          if (existingItemId && !isNaN(existingItemId)) {
            return this.api.updateItem(existingItemId, { ...body, id: existingItemId })
              .pipe(map((r: any) => ({ idx, apiItemId: r.data?.id || existingItemId })));
          }
          return this.api.createItem(body)
            .pipe(map((r: any) => ({ idx, apiItemId: r.data?.id })));
        });

        if (requests.length) {
          forkJoin(requests).subscribe({
            next: (results) => {
              for (const r of results) {
                if (r.apiItemId) data.criteria[r.idx].id = 'c' + r.apiItemId;
              }
              saveLocal();
            },
            error: () => saveLocal()
          });
        } else {
          saveLocal();
        }
      };

      if (this.editTmplId() && existing?.criteria) {
        const currentItemIds = new Set(
          data.criteria
            .map(c => c.id.startsWith('c') ? parseInt(c.id.substring(1)) : null)
            .filter(id => id !== null)
        );
        const deletions = existing.criteria
          .filter(c => c.id.startsWith('c'))
          .map(c => parseInt(c.id.substring(1)))
          .filter(id => !isNaN(id) && !currentItemIds.has(id))
          .map(id => this.api.deleteItem(id));

        if (deletions.length) {
          forkJoin(deletions).subscribe({
            next: () => processItems(),
            error: () => processItems()
          });
        } else {
          processItems();
        }
      } else {
        processItems();
      }
    };

    if (apiId) {
      this.api.updateTemplate(apiId, { id: apiId, name: data.name, calculationType: 1, isActive: true, weeks: this.tWeeks() || 12, term: this.tTerm() }).subscribe({
        next: () => saveItems(apiId),
        error: () => saveLocal(),
      });
    } else {
      if (!this.tSubjectId()) { this.showSnackbar('اختر المادة أولاً'); this.saving.set(false); return; }
      this.api.createTemplate({
        gradeLevelId: this.selectedGradeLevelId(),
        subjectId: this.tSubjectId(),
        academicYearId: this.academicYearId(),
        name: data.name,
        calculationType: 1,
        weeks: this.tWeeks() || 12,
        term: this.tTerm()
      }).subscribe({
        next: (res) => saveItems(res.data.id),
        error: () => saveLocal(),
      });
    }
  }

  delTmpl(id: number) {
    const t = this.templates().find(tm => tm.id === id);
    this.templates.update(list => list.filter(t => t.id !== id));
    this.saveToLocalStorage();
    const apiId = t?.apiId || id;
    this.api.deleteTemplate(apiId).subscribe({ error: () => {} });
    this.showSnackbar('تم حذف القالب');
  }

  // ══ Template getters (like getFinal) — يعزل قراءة الـ signal في ميثود
  getCName(i: number): string { return this.tCriteria()[i]?.name ?? ''; }
  getCMax(i: number): number { return this.tCriteria()[i]?.max ?? 0; }
  getSName(i: number): string { return this.tSumCols()[i]?.name ?? ''; }
  getSMax(i: number): number { return this.tSumCols()[i]?.max ?? 0; }

  onAutoCalcTypeChange(c: Criteria, val: number) {
    c.autoCalcType = val;
    if (val !== 1) c.absenceMax = undefined;
  }

  addCrit() {
    this.tCriteria.update(list => {
      const newIdx = list.filter(c => c.id.startsWith('new_')).length + 1;
      return [...list, { id: 'new_' + newIdx, name: 'عنصر ' + newIdx, max: 10, autoCalcType: 0, absenceMax: undefined }];
    });
  }

  removeCrit(idx: number) {
    this.tCriteria.update(list => list.filter((_, i) => i !== idx));
  }

  // ══ FIX: لا يوجد شرط مقارنة — يحدّث دائماً
  updateCriteriaName(index: number, name: string) {
    const updated = this.tCriteria().map((c, i) => i === index ? { ...c, name } : c);
    this.tCriteria.set(updated);
  }

  updateCriteriaMax(index: number, max: number | string) {
    const numMax = typeof max === 'string' ? parseFloat(max) : max;
    if (isNaN(numMax)) return;
    const updated = this.tCriteria().map((c, i) => i === index ? { ...c, max: numMax } : c);
    this.tCriteria.set(updated);
  }

  updateSumColName(index: number, name: string) {
    const updated = this.tSumCols().map((c, i) => i === index ? { ...c, name } : c);
    this.tSumCols.set(updated);
  }

  updateSumColMax(index: number, max: number | string) {
    const numMax = typeof max === 'string' ? parseFloat(max) : max;
    if (isNaN(numMax)) return;
    const updated = this.tSumCols().map((c, i) => i === index ? { ...c, max: numMax } : c);
    this.tSumCols.set(updated);
  }

  // ══ Event handlers — (input) للـ text, (change) للـ number
  onCriteriaNameInput(index: number, event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.tCriteria.update(list =>
      list.map((c, i) => i === index ? { ...c, name: value } : c)
    );
  }

  onCriteriaMaxInput(index: number, event: Event) {
    const value = (event.target as HTMLInputElement).value;
    const numMax = parseFloat(value);
    if (!isNaN(numMax)) {
      this.tCriteria.update(list =>
        list.map((c, i) => i === index ? { ...c, max: numMax } : c)
      );
    }
  }

  onSumColNameInput(index: number, event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.tSumCols.update(list =>
      list.map((c, i) => i === index ? { ...c, name: value } : c)
    );
  }

  onSumColMaxInput(index: number, event: Event) {
    const value = (event.target as HTMLInputElement).value;
    const numMax = parseFloat(value);
    if (!isNaN(numMax)) {
      this.tSumCols.update(list =>
        list.map((c, i) => i === index ? { ...c, max: numMax } : c)
      );
    }
  }

  addSumCol() {
    this.tSumCols.update(list => [
      ...list,
      { id: 'col_' + (list.length + 1), name: 'عمود ' + (list.length + 1), max: 40 },
    ]);
  }

  removeSumCol(idx: number) {
    this.tSumCols.update(list => list.filter((_, i) => i !== idx));
  }

  // ══════════════════════════════════════
  //  REAL CLASSES (for dropdown)
  // ══════════════════════════════════════
  private loadGrades() {
    this.gradeLevelService.getAll().subscribe({
      next: (data) => {
        const sortedGrades = (data.data ?? data).sort((a: any, b: any) => a.levelOrder - b.levelOrder);
        this.grades.set(sortedGrades);
      }
    });
  }

  private loadApiClasses() {
    this.loadingApiClasses.set(true);
    const obs = this.isTeacherMode()
      ? this.api.getMyClassesCurrentYear()
      : this.api.getClasses();
    obs.subscribe({
      next: (res) => {
        this.apiClasses.set(Array.isArray(res) ? res : (res?.data ?? []));
        this.loadingApiClasses.set(false);
      },
      error: () => this.loadingApiClasses.set(false),
    });
  }

  toggleTeacherMode() {
    this.isTeacherMode.update(v => !v);
    this.loadApiClasses();
    this.loadTeacherSubjects();
  }

  private loadTeacherSubjects() {
    if (!this.isTeacherMode()) return;
    this.api.getMySubjectsCurrentYear().subscribe({
      next: (res) => {
        const data = Array.isArray(res) ? res : (res?.data ?? []);
        if (data.length) this.teacherSubjects.set(data);
      },
    });
  }

  // ══════════════════════════════════════
  //  CLASS-TEMPLATE LINKS (API)
  // ══════════════════════════════════════
  private loadLinksFromApi(): Promise<void> {
    return new Promise(resolve => {
      this.api.getLinks().subscribe({
        next: (res) => {
          const list = Array.isArray(res) ? res : (res?.data ?? []);
          if (list.length) {
            this.classes.set(list.map((link: any) => ({
              linkId: link.id,
              id: link.classId,
              name: link.className || '',
              teacher: link.teacher || '',
              subject: link.subject || '',
              subjectId: link.subjectId,
              year: '',
              template_id: link.templateId,
              students: (link.students || []).map((s: any) => ({
                id: s.id, name: s.name, enrollmentId: s.enrollmentId
              })),
            })));
          } else {
            this.classes.set([]);
          }
          resolve();
        },
        error: () => { resolve(); },
      });
    });
  }

  // ══════════════════════════════════════
  //  PERSISTENCE (localStorage — templates only)
  // ══════════════════════════════════════
  private saveToLocalStorage() {
    try {
      const data = { templates: this.templates() };
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(data));
    } catch {}
  }

  private loadRawFromLocalStorage(): { templates?: any[] } | null {
    try {
      const raw = localStorage.getItem(this.STORAGE_KEY);
      if (!raw) return null;
      return JSON.parse(raw);
    } catch { return null; }
  }

  private loadFromLocalStorage() {
    const data = this.loadRawFromLocalStorage();
    if (!data) return;
    if (data.templates?.length) this.templates.set(data.templates);
  }

  // ══════════════════════════════════════
  //  CLASSES
  // ══════════════════════════════════════
  // ══ Class modal signals ══
  clsModalOpen  = signal(false);
  linkedApiGradeId = signal<number | null>(null);      // selected Grade
  linkedApiClsId = signal<number | null>(null);        // id of selected API class
  cName     = signal('');
  cTeacher  = signal('');
  cSubj     = signal('');
  cYear     = signal('2025/2026');
  cStudents = signal('');
  cTmplId   = signal<number | null>(null);

  // Computed: classes filtered by selected grade
  filteredApiClasses = computed(() => {
    const gradeId = this.linkedApiGradeId();
    if (!gradeId) return [];
    return this.apiClasses().filter(c => c.gradeLevelId === gradeId);
  });

  /** الفصول الظاهرة للمستخدم — لو هو مدرس بنحصرها في فصوله وموادّه بس */
  visibleClasses = computed(() => {
    const all = this.classes();
    if (!this.isTeacherMode()) return all;
    const teacherSubjectNames = new Set(this.teacherSubjects().map(s => s.name));
    return all.filter(c => teacherSubjectNames.has(c.subject));
  });

  openClsForm(c?: ClassItem) {
    this.editClsId.set(c ? c.id : null);
    if (c) {
      this.linkedApiClsId.set(c.id);
      this.cTmplId.set(c.template_id || null);
      const found = this.apiClasses().find((ac: any) => ac.id === c.id);
      if (found) this.linkedApiGradeId.set(found.gradeLevelId);
    } else {
      this.linkedApiGradeId.set(null);
      this.linkedApiClsId.set(null);
      this.cTmplId.set(null);
    }
    // Look up class info from apiClasses (not from link data) for consistency
    const apiId = c?.id;
    const found = apiId ? this.apiClasses().find((ac: any) => ac.id === apiId) : null;
    this.cName.set(found?.name ?? c?.name ?? '');
    this.cTeacher.set(found?.gradeLevelName ?? '');
    this.cSubj.set(found?.academicYearName ?? '');
    this.cYear.set('');
    this.cStudents.set('');
    this.clsModalOpen.set(true);
  }

  onLinkedApiGradeChange(id: number | null) {
    this.linkedApiGradeId.set(id);
    this.linkedApiClsId.set(null);
    this.cName.set('');
    this.cTeacher.set('');
    this.cSubj.set('');
    this.cYear.set('');
    this.cStudents.set('');
    this.cTmplId.set(null);
  }

  onLinkedApiClsChange(id: number | null) {
    this.linkedApiClsId.set(id);
    if (id == null) return;
    const found = this.apiClasses().find((c: any) => c.id === Number(id));
    if (found) {
      this.cName.set(found.name || '');
      this.cTeacher.set(found.gradeLevelName || '');
      this.cSubj.set(found.academicYearName || '');
      this.cYear.set('');
      this.cStudents.set('');
      this.cTmplId.set(null);
    }
  }

  saveCls() {
    if (this.cTmplId() === null) return;

    const apiClsId = this.linkedApiClsId();
    if (apiClsId == null) return;
    this.api.createLink({ classId: Number(apiClsId), templateId: this.cTmplId()!, academicYearId: this.academicYearId() }).subscribe({
      next: () => {
        this.loadLinksFromApi();
        this.clsModalOpen.set(false);
        this.showSnackbar('تم ربط الفصل بالقالب');
      },
      error: () => this.showSnackbar('فشل الربط'),
    });
  }

  delCls(id: number) {
    const item = this.visibleClasses().find(c => c.id === id);
    if (!item || !item.linkId) return;
    this.api.deleteLink(item.linkId).subscribe({
      next: () => {
        this.loadLinksFromApi();
        this.showSnackbar('تم فك الربط');
      },
      error: () => this.showSnackbar('فشل فك الربط'),
    });
  }

  // ══════════════════════════════════════
  //  COMPUTED
  // ══════════════════════════════════════
  currentTmpl = computed(() => {
    // إذا تم اختيار قالب يدوي، استخدمه
    const manualTmplId = this.entryTemplateId();
    if (manualTmplId) {
      return this.templates().find(t => t.id === manualTmplId) || null;
    }
    const cid = this.entryClassId();
    if (!cid) return null;
    const cls = this.visibleClasses().find(c => c.id === Number(cid));
    if (!cls) return null;
    return this.templates().find(t => t.id === cls.template_id)
      || (cls.subject ? this.templates().find(t => t.subjectName === cls.subject) : null)
      || null;
  });

  currentCls = computed(() => {
    const cid = this.entryClassId();
    if (!cid) return null;
    return this.visibleClasses().find(c => c.id === Number(cid)) || null;
  });

  weeklyMax = computed(() => {
    const tmpl = this.currentTmpl();
    return tmpl ? tmpl.criteria.reduce((a, c) => a + c.max, 0) : 40;
  });

  stats = computed(() => ({
    classes: this.apiClasses().length,
    students: this.studentCount(),
    entries: this.calculateTotalEntries(),
    templates: this.templates().length,
  }));

  private calculateTotalEntries(): number {
    // Rough count: number of grade entries across all loaded evaluations
    return Object.keys(this.gradeValues()).length + Object.keys(this.absenceValues()).length;
  }

  recentClasses = computed(() => this.classes().slice(0, 5));

  // ══ Top Students / Needing Support ══
  topStudentsData = signal<any[]>([]);
  needingSupportData = signal<any[]>([]);
  loadingTopStudents = signal(false);
  loadingNeedingSupport = signal(false);
  topStudentsModalOpen = signal(false);
  needingSupportModalOpen = signal(false);

  loadTopStudents(classId: number) {
    this.loadingTopStudents.set(true);
    this.api.recalculateFinalGrades(classId, this.entryTerm()).subscribe({
      next: () => {
        this.api.getTopStudents(classId, 10, this.entryTerm()).subscribe({
          next: (res) => {
            const data = res?.isSuccess ? res.data : (Array.isArray(res) ? res : []);
            this.topStudentsData.set(data ?? []);
            this.topStudentsModalOpen.set(true);
            this.loadingTopStudents.set(false);
          },
          error: () => this.loadingTopStudents.set(false),
        });
      },
      error: () => this.loadingTopStudents.set(false),
    });
  }

  loadNeedingSupport(classId: number) {
    this.loadingNeedingSupport.set(true);
    this.api.recalculateFinalGrades(classId, this.entryTerm()).subscribe({
      next: () => {
        this.api.getStudentsNeedingSupport(classId, 50, this.entryTerm()).subscribe({
          next: (res) => {
            const data = res?.isSuccess ? res.data : (Array.isArray(res) ? res : []);
            this.needingSupportData.set(data ?? []);
            this.needingSupportModalOpen.set(true);
            this.loadingNeedingSupport.set(false);
          },
          error: () => this.loadingNeedingSupport.set(false),
        });
      },
      error: () => this.loadingNeedingSupport.set(false),
    });
  }

  // ══════════════════════════════════════
  //  NAVIGATION
  // ══════════════════════════════════════
  sections = ['dash', 'tmpl', 'cls', 'entry', 'exp'] as const;

  setSection(s: string) {
    if (this.sections.includes(s as any)) {
      this.activeSection.set(s as any);
    }
  }

  goEntry(cid: number) {
    // إلغاء أي طلب HTTP جاري قبل تغيير الفصل
    this.cancelLoad$.next();
    this.entryClassId.set(+cid);
    this.entryWeek.set(1);
    this.entryTemplateId.set(null); // إعادة تعيين القالب اليدوي

    // تعيين الترم تلقائياً من القالب المرتبط
    const cls = this.visibleClasses().find(c => c.id === Number(cid));
    const tmpl = cls ? (this.templates().find(t => t.id === cls.template_id) || null) : null;
    this.entryTerm.set(this.normalizeTerm(tmpl?.term) ?? 1);

    this.activeSection.set('entry');
    this.clearGrades();
    if (this.dataReady()) this.loadEvaluations();
    else this.pendingReload.set(true);
  }

  /** القوالب المتاحة للفصل المختار */
  availableTemplatesForEntry = computed(() => {
    const cid = this.entryClassId();
    if (!cid) return [];
    const cls = this.visibleClasses().find(c => c.id === Number(cid));
    if (!cls) return [];
    // ابحث عن القالب المرتبط أولاً، ثم كل القوالب التي لها نفس المادة
    const linked = this.templates().find(t => t.id === cls.template_id);
    const bySubject = cls.subject
      ? this.templates().filter(t => t.subjectName === cls.subject)
      : [];
    const result = linked ? [linked, ...bySubject.filter(t => t.id !== linked.id)] : bySubject;
    return result.length ? result : this.templates();
  });

  // ══ يُستدعى من الـ HTML عند تغيير الأسبوع ══
  onWeekChange(event: Event) {
    const value = Number((event.target as HTMLSelectElement).value);
    this.entryWeek.set(value);
    this.onClassOrWeekChange();
  }

  // ══ يُستدعى من الـ HTML عند تغيير الأسبوع أو الفصل ══
  onClassOrWeekChange() {
    // إلغاء أي طلب HTTP جاري
    this.cancelLoad$.next();
    // إذا لم يتم اختيار قالب يدوي، اختر أول قالب متاح تلقائياً
    if (!this.entryTemplateId()) {
      const avail = this.availableTemplatesForEntry();
      if (avail.length) this.entryTemplateId.set(avail[0].id);
    }
    // امسح البيانات القديمة
    this.gradeValues.set({});
    this.absenceValues.set({});
    this.existingEvalMap.set({});
    this.enrollmentMap.set({});
    this.loadEvaluations();
  }

  goExp(cid: number) {
    this.exportClassId.set(cid);
    this.activeSection.set('exp');
  }

  // ══════════════════════════════════════
  //  WEEK HELPERS
  // ══════════════════════════════════════

  /** Normalize AcademicTerm string/number to a number (1=First, 2=Second) */
  private normalizeTerm(val: any): number | null | undefined {
    if (val == null || val === undefined) return val;
    if (typeof val === 'number') return val;
    if (typeof val === 'string') {
      const map: Record<string, number> = { FirstSemester: 1, SecondSemester: 2, Final: 3, الأول: 1, الثاني: 2 };
      if (val in map) return map[val];
      const n = Number(val);
      if (!isNaN(n)) return n;
    }
    return null;
  }

  /** Weekly periods filtered by current term (if any) */
  private termWeeks = computed(() => {
    const term = this.entryTerm();
    let periods = this.periods().filter(p => p.periodType === 'Weekly');
    if (term != null) {
      periods = periods.filter(p => p.semesterNumber == null || p.semesterNumber === term);
    }
    return periods;
  });

  entryWeeks = computed(() => {
    const periods = this.termWeeks();
    const tmpl = this.currentTmpl();
    if (!tmpl) {
      return periods.length > 0
        ? Array.from({ length: periods.length }, (_, i) => i + 1)
        : [];
    }
    const maxWeeks = Math.min(tmpl.weeks, periods.length);
    return Array.from({ length: maxWeeks }, (_, i) => i + 1);
  });

  weekLabel(wn: number): string {
    const periods = this.termWeeks();
    const idx = Number(wn) - 1;
    if (idx >= 0 && idx < periods.length) {
      const period = periods[idx];
      const pDate = period.startDate ? ' (' + period.startDate + ')' : '';
      return 'الأسبوع ' + (idx + 1) + pDate;
    }
    const tmpl = this.currentTmpl();
    if (!tmpl) return 'الأسبوع ' + wn;
    const num = Number(wn);
    const wName = tmpl.week_names[num - 1] || num;
    const wDate = tmpl.week_dates[num - 1] ? ' (' + tmpl.week_dates[num - 1] + ')' : '';
    return 'الأسبوع ' + wName + wDate;
  }

  private getPeriodId(weekNum: number): number | null {
    const periods = this.termWeeks();
    const idx = Number(weekNum) - 1;
    if (idx >= 0 && idx < periods.length) {
      return periods[idx].id ?? null;
    }
    return null;
  }

  exportTmpl = computed(() => {
    const cid = this.exportClassId();
    if (!cid) return null;
    const cls = this.visibleClasses().find(c => c.id === Number(cid));
    if (!cls) return null;
    return this.templates().find(t => t.id === cls.template_id)
      || (cls.subject ? this.templates().find(t => t.subjectName === cls.subject) : null)
      || null;
  });

  exportCls = computed(() => {
    const cid = this.exportClassId();
    if (!cid) return null;
    return this.visibleClasses().find(c => c.id === Number(cid)) || null;
  });

  getExportMonthAverage(studentId: number, monthGroup: { name: string; weeks: number[] }): string {
    let total = 0; let count = 0;
    const tmpl = this.exportTmpl();
    if (!tmpl) return '—';
    for (const w of monthGroup.weeks) {
      for (const c of tmpl.criteria) {
        const val = this.gradeValues()[studentId + '_' + w + '_' + c.id];
        if (val != null) { total += val; count++; }
      }
    }
    return count > 0 ? (total / count).toFixed(1) : '—';
  }

  getExportTotalAverage(studentId: number): string {
    const tmpl = this.exportTmpl();
    if (!tmpl) return '—';
    let total = 0; let count = 0;
    for (const mg of tmpl.month_groups) {
      for (const w of mg.weeks) {
        for (const c of tmpl.criteria) {
          const val = this.gradeValues()[studentId + '_' + w + '_' + c.id];
          if (val != null) { total += val; count++; }
        }
      }
    }
    return count > 0 ? (total / count).toFixed(1) : '—';
  }

  // ══════════════════════════════════════
  //  GRADE / ABSENCE / EXAM ENTRY
  // ══════════════════════════════════════
  gradeValues = signal<Record<string, number>>({});
  absenceValues = signal<Record<string, boolean>>({});
  examValues = signal<Record<string, number>>({});
  finalValues = signal<Record<string, number>>({});

  getStudents() {
    return this.currentCls()?.students || [];
  }

  private normalizeHalfGrade(v: number): number {
    return Math.ceil(v * 2) / 2;
  }

  setGrade(sid: number, cid: string, val: number | string) {
    const wn = Number(this.entryWeek());
    const numVal = typeof val === 'string' ? parseFloat(val) : val;
    if (isNaN(numVal)) return;
    const normalized = this.normalizeHalfGrade(numVal);
    this.localGrades[sid + '_' + cid] = normalized;
    this.gradeValues.update(m => ({ ...m, [sid + '_' + wn + '_' + cid]: normalized }));
  }

  gradeTotal(sid: number): number {
    const wn = Number(this.entryWeek());
    const tmpl = this.currentTmpl();
    if (!tmpl) return 0;
    const total = tmpl.criteria.reduce((sum, c) =>
      sum + (this.gradeValues()[sid + '_' + wn + '_' + c.id] || 0), 0);
    return Math.round(total * 10) / 10;
  }

  toggleAbs(sid: number, day: string) {
    const wn = Number(this.entryWeek());
    const key = sid + '_' + wn + '_' + day;
    this.localAbsences[sid + '_' + day] = !this.localAbsences[sid + '_' + day];
    this.absenceValues.update(m => ({ ...m, [key]: this.localAbsences[sid + '_' + day] }));
  }

  isAbsent(sid: number, day: string): boolean {
    return !!this.localAbsences[sid + '_' + day];
  }

  absenceCount(sid: number): number {
    const tmpl = this.currentTmpl();
    if (!tmpl) return 0;
    return tmpl.absent_days.filter(d => this.isAbsent(sid, d)).length;
  }

  setExam(sid: number, examNum: number, val: number | string) {
    const numVal = typeof val === 'string' ? parseFloat(val) : val;
    if (isNaN(numVal)) return;
    const normalized = this.normalizeHalfGrade(numVal);
    this.localExams[sid + '_e' + examNum] = normalized;
    this.examValues.update(m => ({ ...m, [sid + '_e' + examNum]: normalized }));
  }

  getExam(sid: number, examNum: number): number {
    return this.examValues()[sid + '_e' + examNum] || 0;
  }

  setFinal(sid: number, field: string, val: number | string) {
    const numVal = typeof val === 'string' ? parseFloat(val) : val;
    if (isNaN(numVal)) return;
    const normalized = this.normalizeHalfGrade(numVal);
    this.localFinals[sid + '_' + field] = normalized;
    this.finalValues.update(m => ({ ...m, [sid + '_' + field]: normalized }));
  }

  getFinal(sid: number, field: string): number {
    return this.finalValues()[sid + '_' + field] || 0;
  }

  finalWritten(sid: number): number {
    return Math.round((this.getFinal(sid, 'ma') + this.getFinal(sid, 'e1') + this.getFinal(sid, 'e2')) * 10) / 10;
  }

  finalTotal(sid: number): number {
    return Math.round((this.finalWritten(sid) + this.getFinal(sid, 'fe')) * 10) / 10;
  }

  isFinalComplete(sid: number): boolean {
    return this.finalCompleteStatus()[sid] !== false;
  }

  finalMaxTotal(sid: number): number {
    return this.finalMaxValues()[sid] ?? 100;
  }

  entrySummaryText = computed(() => {
    const type = this.entryType();
    if (type === 'grades') {
      const vals = Object.values(this.gradeValues()).filter(v => v > 0);
      return 'حقول مملوءة: ' + vals.length + ' · متوسط: ' + (vals.length ? (vals.reduce((a, b) => a + b, 0) / this.getStudents().length).toFixed(1) : '—');
    }
    if (type === 'absence') {
      return 'إجمالي الغياب: ' + Object.values(this.absenceValues()).filter(v => v).length;
    }
    return '';
  });

  fillZeros() {
    const students = this.getStudents();
    const tmpl = this.currentTmpl();
    if (!tmpl) return;
    students.forEach(st => tmpl.criteria.forEach(c => this.setGrade(st.id, c.id, 0)));
  }

  fillMax() {
    const students = this.getStudents();
    const tmpl = this.currentTmpl();
    if (!tmpl) return;
    students.forEach(st => tmpl.criteria.forEach(c => this.setGrade(st.id, c.id, c.max)));
  }

  // ══════════════════════════════════════
  //  LOAD EVALUATIONS FROM API
  //  يُستدعى عند تغيير الفصل أو الأسبوع
  // ══════════════════════════════════════
  loadEvaluations() {
    // امسح أي بيانات قديمة أولاً (مهم جداً قبل أي early return)
    this.clearGradesInternal();

    if (!this.dataReady()) { this.pendingReload.set(true); return; }
    const cls = this.currentCls();
    const tmpl = this.currentTmpl();
    const wn = Number(this.entryWeek());
    if (!cls || !tmpl) return;

    // ① إلغاء أي طلب HTTP سابق (race condition fix)
    this.cancelLoad$.next();
    this.loadingWeek.set(true);

    const type = this.entryType();

    if (type === 'grades') {
      this.loadWeeklyGrades(cls, tmpl, wn);
    } else if (type === 'absence') {
      this.loadWeeklyAbsences(cls, tmpl, wn);
    } else if (type === 'exams') {
      this.loadExams(cls, tmpl);
    } else if (type === 'final') {
      this.loadFinalGrades(cls);
    }
  }

  private clearGradesInternal() {
    this.gradeValues.set({});
    this.absenceValues.set({});
    this.examValues.set({});
    this.finalValues.set({});
    this.enrollmentMap.set({});
    this.existingEvalMap.set({});
    this.localGrades = {};
    this.localAbsences = {};
    this.localExams = {};
    this.localFinals = {};
    this.finalCompleteStatus.set({});
    this.finalMaxValues.set({});
    this.finalTouched.clear();
    this.absenceSnapshot = {};
  }

  private loadWeeklyGrades(cls: ClassItem, tmpl: Template, wn: number) {
    const periodId = this.currentPeriodId();
    if (!periodId) { this.loadingWeek.set(false); return; }

    const tmplItemIds = new Set(
      tmpl.criteria
        .map(c => parseInt(c.id.replace('c', '')))
        .filter(n => !isNaN(n))
    );

    this.api.getEvaluationsByClassPeriod(cls.id, periodId)
      .pipe(takeUntil(this.cancelLoad$))
      .subscribe({
        next: (res) => {
          this.loadingWeek.set(false);
          if (!res.isSuccess || !res.data?.length) return;

          const classEvals = res.data as any[];
          const grades: Record<string, number> = {};
          const evalIds: Record<string, number> = {};
          const enrollMap: Record<number, number> = {};

          for (const ce of classEvals) {
            const st = cls.students.find(s => s.name === ce.studentName)
              || cls.students.find(s => (s.enrollmentId ?? s.id) === ce.enrollmentId);
            const eid = ce.enrollmentId;
            if (st) enrollMap[st.id] = eid;

            for (const ev of (ce.evaluations || [])) {
              if (ev.score == null || !st) continue;
              if (!tmplItemIds.has(ev.evaluationItemId)) continue;
              const key = st.id + '_' + wn + '_c' + ev.evaluationItemId;
              grades[key] = ev.score;
              this.localGrades[st.id + '_c' + ev.evaluationItemId] = ev.score;
              evalIds[eid + '_' + ev.evaluationItemId] = ev.id;
            }
          }

          if (Object.keys(grades).length) this.gradeValues.set(grades);
          if (Object.keys(evalIds).length) this.existingEvalMap.set(evalIds);
          if (Object.keys(enrollMap).length) this.enrollmentMap.set(enrollMap);

          this.loadAbsences(cls, tmpl, wn, periodId, enrollMap);
        },
        error: () => { this.loadingWeek.set(false); }
      });
  }

  private loadExams(cls: ClassItem, tmpl: Template) {
    const students = cls.students;
    if (!students.length) { this.loadingWeek.set(false); return; }

    const subjectId = cls.subjectId ?? tmpl.subjectId;

    this.api.getAssessmentsByClass(cls.id, this.entryTerm(), subjectId)
      .pipe(takeUntil(this.cancelLoad$))
      .subscribe({
        next: (res) => {
          this.loadingWeek.set(false);
          if (!res.isSuccess || !res.data?.length) return;

          const assessments = res.data as any[];
          const exams: Record<string, number> = {};

          for (const a of assessments) {
            const st = students.find(s =>
              (s.enrollmentId ?? s.id) === a.enrollmentId
            );
            if (!st) continue;

            // assessmentType is a string like "MonthlyExam1", "MonthlyExam2" due to JsonStringEnumConverter
            const examNum = a.assessmentType === 'MonthlyExam1' ? 1
                          : a.assessmentType === 'MonthlyExam2' ? 2
                          : null;
            if (examNum == null) continue;

            const enrollKey = st.enrollmentId ?? st.id;
            const key = enrollKey + '_e' + examNum;
            exams[key] = a.score;
            this.localExams[key] = a.score;
          }

          if (Object.keys(exams).length) this.examValues.set(exams);
        },
        error: () => { this.loadingWeek.set(false); }
      });
  }

  private loadFinalGrades(cls: ClassItem) {
    const students = cls.students;
    if (!students.length) { this.loadingWeek.set(false); return; }

    // دايمًا نعيد الحساب الأول عشان النتايج تكون محدثة (بدل ما نجيب القديم)
    this.autoCalcFinalGrades(cls);
  }

  private autoCalcFinalGrades(cls: ClassItem) {
    const subjectId = cls.subjectId ?? (this.currentTmpl()?.subjectId);
    this.api.recalculateFinalGrades(cls.id, this.entryTerm(), subjectId).subscribe({
      next: (res) => {
        this.loadingWeek.set(false);
        if (res.isSuccess && res.data?.length) {
          this.processFinalGradesResponse(res.data, cls);
        }
      },
      error: () => { this.loadingWeek.set(false); }
    });
  }

  private processFinalGradesResponse(finals: any[], cls: ClassItem) {
    const students = cls.students;
    const gradeMap: Record<string, number> = {};
    const completeMap: Record<string, boolean> = {};
    const maxMap: Record<string, number> = {};

    for (const f of finals) {
      const st = students.find(s =>
        (s.enrollmentId ?? s.id) === f.enrollmentId
      );
      if (!st) continue;

      const enrollKey = st.enrollmentId ?? st.id;

      // لو فيه duplicate records، نفضل اللي عنده maxTotal أكبر
      const existingMax = maxMap[enrollKey];
      const currentMax = f.maxTotal ?? 0;
      if (existingMax !== undefined && currentMax < existingMax) {
        continue;
      }

      completeMap[enrollKey] = f.isComplete !== false;
      maxMap[enrollKey] = f.maxTotal ?? 100;

      if (f.periodAvgScore > 0 || f.isComplete) {
        this.localFinals[enrollKey + '_ma'] = f.periodAvgScore ?? 0;
        gradeMap[enrollKey + '_ma'] = f.periodAvgScore ?? 0;
      }
      if (f.assessment1Score > 0 || f.isComplete) {
        this.localFinals[enrollKey + '_e1'] = f.assessment1Score ?? 0;
        gradeMap[enrollKey + '_e1'] = f.assessment1Score ?? 0;
      }
      if (f.assessment2Score > 0 || f.isComplete) {
        this.localFinals[enrollKey + '_e2'] = f.assessment2Score ?? 0;
        gradeMap[enrollKey + '_e2'] = f.assessment2Score ?? 0;
      }
      if (f.finalExamScore > 0 || f.isComplete) {
        this.localFinals[enrollKey + '_fe'] = f.finalExamScore ?? 0;
        gradeMap[enrollKey + '_fe'] = f.finalExamScore ?? 0;
      }
    }

    if (Object.keys(gradeMap).length) this.finalValues.set(gradeMap);
    this.finalCompleteStatus.set(completeMap);
    this.finalMaxValues.set(maxMap);
  }

  private loadWeeklyAbsences(cls: ClassItem, tmpl: Template, wn: number) {
    const periodId = this.currentPeriodId();
    if (!periodId || tmpl.absent_days.length === 0) { this.loadingWeek.set(false); return; }

    const period = this.periods().find(p => p.id === periodId);
    if (!period || !period.startDate || !period.endDate) { this.loadingWeek.set(false); return; }

    const students = cls.students;
    const enrollmentIds: number[] = [];
    const stIdByEnrollment: Record<number, number> = {};
    for (const st of students) {
      const enrollmentId = st.enrollmentId ?? st.id;
      enrollmentIds.push(enrollmentId);
      stIdByEnrollment[enrollmentId] = st.id;
    }
    if (enrollmentIds.length === 0) { this.loadingWeek.set(false); return; }

    const firstDay = period.startDate;
    const lastDay = period.endDate;

    this.api.getAbsencesByEnrollments(enrollmentIds, firstDay, lastDay)
      .pipe(takeUntil(this.cancelLoad$))
      .subscribe({
        next: (res) => {
          this.loadingWeek.set(false);
          if (!res.isSuccess || !res.data?.length) return;
          const absences: Record<string, boolean> = {};
          for (const a of (res.data as any[])) {
            const dayKey = this.getDayKeyForDate(a.absenceDate, tmpl);
            if (!dayKey) continue;
            const stId = stIdByEnrollment[a.enrollmentId];
            if (!stId) continue;
            const key = stId + '_' + wn + '_' + dayKey;
            absences[key] = a.isAbsent;
            this.localAbsences[stId + '_' + dayKey] = a.isAbsent;
          }
          if (Object.keys(absences).length) {
            this.absenceValues.update(m => ({ ...m, ...absences }));
            this.absenceSnapshot = { ...absences };
          }
        },
        error: () => { this.loadingWeek.set(false); }
      });
  }

  private loadAbsences(cls: ClassItem, tmpl: Template, wn: number, periodId: number, enrollMap: Record<number, number>) {
    if (tmpl.absent_days.length === 0) return;
    const students = cls.students;
    const enrollmentIds: number[] = [];
    const stIdByEnrollment: Record<number, number> = {};
    for (const st of students) {
      const enrollmentId = st.enrollmentId ?? enrollMap[st.id] ?? st.id;
      enrollmentIds.push(enrollmentId);
      stIdByEnrollment[enrollmentId] = st.id;
    }
    if (enrollmentIds.length === 0) return;

    const period = this.periods().find(p => p.id === periodId);
    if (!period || !period.startDate || !period.endDate) return;
    const firstDay = period.startDate;
    const lastDay = period.endDate;
    this.api.getAbsencesByEnrollments(enrollmentIds, firstDay, lastDay).subscribe({
      next: (res) => {
        if (!res.isSuccess || !res.data?.length) return;
        const absences: Record<string, boolean> = {};
        for (const a of (res.data as any[])) {
          const dayKey = this.getDayKeyForDate(a.absenceDate, tmpl);
          if (!dayKey) continue;
          const stId = stIdByEnrollment[a.enrollmentId];
          if (!stId) continue;
          const key = stId + '_' + wn + '_' + dayKey;
          absences[key] = a.isAbsent;
          this.localAbsences[stId + '_' + dayKey] = a.isAbsent;
        }
        if (Object.keys(absences).length) {
          this.absenceValues.update(m => ({ ...m, ...absences }));
          this.absenceSnapshot = { ...absences };
        }
      }
    });
  }

  private getDayKeyForDate(dateStr: string, tmpl: Template): string | null {
    const date = new Date(dateStr);
    const dayNames = ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];
    const dayName = dayNames[date.getDay()];
    return tmpl.absent_days.includes(dayName) ? dayName : null;
  }

  clearGrades() {
    this.gradeValues.set({});
    this.absenceValues.set({});
    this.examValues.set({});
    this.finalValues.set({});
    this.enrollmentMap.set({});
    this.existingEvalMap.set({});
    this.localGrades = {};
    this.localAbsences = {};
    this.localExams = {};
    this.localFinals = {};
    this.absenceSnapshot = {};
  }

  onGradeChange(sid: number, cid: string, val: any) {
    const key = sid + '_' + cid;
    this.localGrades[key] = val;
    const wn = Number(this.entryWeek());
    if (val === null || val === undefined || val === '') {
      this.gradeValues.update(m => {
        const copy = { ...m };
        delete copy[sid + '_' + wn + '_' + cid];
        return copy;
      });
      return;
    }
    const num = parseFloat(val);
    if (!isNaN(num)) {
      this.gradeValues.update(m => ({ ...m, [sid + '_' + wn + '_' + cid]: num }));
    }
  }

  onExamChange(st: any, examNum: number, val: any) {
    const key = (st.enrollmentId ?? st.id) + '_e' + examNum;
    this.localExams[key] = val;
    if (val === null || val === undefined || val === '') {
      this.examValues.update(m => {
        const copy = { ...m };
        delete copy[key];
        return copy;
      });
      return;
    }
    const num = parseFloat(val);
    if (!isNaN(num)) {
      this.examValues.update(m => ({ ...m, [key]: num }));
    }
  }

  onFinalChange(st: any, field: string, val: any) {
    const key = (st.enrollmentId ?? st.id) + '_' + field;
    this.finalTouched.add(key);
    this.localFinals[key] = val;
    if (val === null || val === undefined || val === '') {
      this.finalValues.update(m => {
        const copy = { ...m };
        delete copy[key];
        return copy;
      });
      return;
    }
    const num = parseFloat(val);
    if (!isNaN(num)) {
      this.finalValues.update(m => ({ ...m, [key]: num }));
    }
  }

  getGradeDisplay(sid: number, cid: string): string {
    const key = sid + '_' + Number(this.entryWeek()) + '_' + cid;
    const v = this.gradeValues()[key];
    return v !== undefined ? String(v) : '';
  }

  getExamDisplay(sid: number, examNum: number): string {
    const v = this.getExam(sid, examNum);
    return v ? String(v) : '';
  }

  getFinalDisplay(sid: number, field: string): string {
    const v = this.getFinal(sid, field);
    return v ? String(v) : '';
  }

  // ══════════════════════════════════════
  //  SAVE TO API — حفظ كامل مع create/update
  // ══════════════════════════════════════
  saveGrades() {
    const cls = this.currentCls();
    const tmpl = this.currentTmpl();
    const wn = Number(this.entryWeek());
    if (!cls || !tmpl) return;

    this.saving.set(true);
    const type = this.entryType();
    const periodId = this.currentPeriodId();
    if (!periodId && (type === 'grades' || type === 'absence')) {
      this.showSnackbar('لم يتم العثور على معرف الفترة');
      this.saving.set(false);
      return;
    }

    if (type === 'grades') {
      const students = this.getStudents();
      const evalMap = this.existingEvalMap();
      const enrollMap = this.enrollmentMap();

      const entries: {
        evaluationId?: number;
        enrollmentId: number;
        evaluationItemId: number;
        periodId: number;
        score: number | null;
      }[] = [];

      for (const st of students) {
        const enrollmentId = st.enrollmentId ?? enrollMap[st.id] ?? st.id;
        for (const c of tmpl.criteria) {
          const rawScore = this.gradeValues()[st.id + '_' + wn + '_' + c.id];
          if (rawScore == null) continue;

          const evalItemId = parseInt(c.id.replace('c', ''));
          if (isNaN(evalItemId)) continue;

          // Normalize score to half-grade increments before sending
          const score = this.normalizeHalfGrade(rawScore);
          if (score > c.max) continue;

          const entry: any = {
            enrollmentId,
            evaluationItemId: evalItemId,
            periodId,
            score,
          };

          const existingId = evalMap[enrollmentId + '_' + evalItemId];
          if (existingId) entry.evaluationId = existingId;

          entries.push(entry);
        }
      }

      if (entries.length) {
        this.api.bulkSaveEvaluations(entries).subscribe({
          next: () => {
            this.saving.set(false);
            this.showSnackbar('تم حفظ الدرجات بنجاح ✓');
            this.loadEvaluations();
          },
          error: () => {
            this.saving.set(false);
            this.showSnackbar('حدث خطأ أثناء الحفظ');
          }
        });
      } else {
        this.saving.set(false);
        this.showSnackbar('لا توجد تغييرات للحفظ');
      }

    } else if (type === 'absence') {
      const students = this.getStudents();
      const enrollMap = this.enrollmentMap();
      const requests: any[] = [];
      const sentKeys: string[] = [];

      for (const st of students) {
        const enrollmentId = st.enrollmentId ?? enrollMap[st.id] ?? st.id;
        for (const day of tmpl.absent_days) {
          const key = st.id + '_' + wn + '_' + day;
          const isAbsent = this.absenceValues()[key];
          if (isAbsent == null) continue;
          if (this.absenceSnapshot[key] === isAbsent) continue;
          requests.push(
            this.api.recordAbsence({
              enrollmentId,
              absenceDate: this.getDateForDay(wn, day),
              isAbsent,
              periodId,
            })
          );
          sentKeys.push(key);
        }
      }

      if (requests.length) {
        forkJoin(requests).subscribe({
          next: () => {
            this.saving.set(false);
            this.showSnackbar('تم حفظ الغياب بنجاح ✓');
            for (const k of sentKeys) this.absenceSnapshot[k] = this.absenceValues()[k] ?? false;
          },
          error: () => {
            this.saving.set(false);
            this.showSnackbar('حدث خطأ أثناء حفظ الغياب');
          }
        });
      } else {
        this.saving.set(false);
        this.showSnackbar('لا توجد تغييرات للحفظ');
      }

    } else if (type === 'exams') {
      const students = this.getStudents();
      const enrollMap = this.enrollmentMap();
      const classId = cls.id;
      const subjectId = cls.subjectId ?? this.currentTmpl()?.subjectId;

      // أولا نجلب التقييمات الموجودة لنعرف ماذا ننشئ وماذا نحدث
      this.api.getAssessmentsByClass(classId, this.entryTerm(), subjectId).pipe(takeUntil(this.cancelLoad$)).subscribe({
        next: (existingRes) => {
          const existingMap: Record<string, number> = {};
          if (existingRes.isSuccess && existingRes.data?.length) {
            for (const a of existingRes.data) {
              // assessmentType is a string like "MonthlyExam1" due to JsonStringEnumConverter
              // Normalize to number for consistent key matching
              const typeNum = a.assessmentType === 'MonthlyExam1' ? 1
                            : a.assessmentType === 'MonthlyExam2' ? 2
                            : a.assessmentType === 'InitialAssessment' ? 3
                            : a.assessmentType === 'FinalAssessment' ? 4
                            : a.assessmentType === 'SemesterExam' ? 5
                            : null;
              if (typeNum == null) continue;
              existingMap[a.enrollmentId + '_' + typeNum] = a.id;
            }
          }

          const requests: any[] = [];
          for (const st of students) {
            const enrollmentId = st.enrollmentId ?? enrollMap[st.id] ?? st.id;
            for (let examNum = 1; examNum <= 2; examNum++) {
              const lookupKey = (st.enrollmentId ?? st.id) + '_e' + examNum;
              const rawScore = this.localExams[lookupKey];
              if (rawScore == null || rawScore === '') continue;
              const score = this.normalizeHalfGrade(parseFloat(rawScore));
              if (isNaN(score)) continue;

              const existingId = existingMap[enrollmentId + '_' + examNum];
              if (existingId) {
                // موجود → تحديث
                requests.push(
                  this.api.updateAssessment({
                    assessmentId: existingId,
                    score,
                  })
                );
              } else {
                // جديد → إنشاء
                requests.push(
                  this.api.recordAssessment({
                    enrollmentId,
                    subjectId,
                    assessmentType: examNum,
                    score,
                    maxScore: 15,
                  })
                );
              }
            }
          }

          if (requests.length) {
            forkJoin(requests).subscribe({
              next: () => {
                this.saving.set(false);
                this.showSnackbar('تم حفظ الامتحانات بنجاح ✓');
                this.loadExams(cls, this.currentTmpl()!);
              },
              error: () => {
                this.saving.set(false);
                this.showSnackbar('حدث خطأ أثناء حفظ الامتحانات');
              }
            });
          } else {
            this.saving.set(false);
            this.showSnackbar('لا توجد تغييرات للحفظ');
          }
        },
        error: () => {
          this.saving.set(false);
          this.showSnackbar('حدث خطأ أثناء جلب التقييمات الموجودة');
        }
      });

    } else if (type === 'final') {
      const classId = cls.id;
      const subjectId = cls.subjectId ?? this.currentTmpl()?.subjectId;
      const enrollMap = this.enrollmentMap();
      const students: { enrollmentId: number; monthlyExam1Score?: number | null; monthlyExam2Score?: number | null; semesterExamScore?: number | null }[] = [];

      for (const st of cls.students) {
        const enrollmentId = st.enrollmentId ?? enrollMap[st.id] ?? st.id;
        const enrollKey = st.enrollmentId ?? st.id;
        const feRaw = this.localFinals[enrollKey + '_fe'];
        const e1Raw = this.localFinals[enrollKey + '_e1'];
        const e2Raw = this.localFinals[enrollKey + '_e2'];

        const entry: any = { enrollmentId };

        if (feRaw != null && feRaw !== '') {
          const feScore = this.normalizeHalfGrade(parseFloat(feRaw));
          if (!isNaN(feScore)) entry.semesterExamScore = feScore;
        }
        if (e1Raw != null && e1Raw !== '') {
          const e1Score = this.normalizeHalfGrade(parseFloat(e1Raw));
          if (!isNaN(e1Score)) entry.monthlyExam1Score = e1Score;
        }
        if (e2Raw != null && e2Raw !== '') {
          const e2Score = this.normalizeHalfGrade(parseFloat(e2Raw));
          if (!isNaN(e2Score)) entry.monthlyExam2Score = e2Score;
        }

        students.push(entry);
      }

      this.api.calculateFullFinalGrades(classId, { students, term: this.entryTerm(), subjectId }).subscribe({
        next: (res) => {
          this.saving.set(false);
          if (res.isSuccess) {
            this.showSnackbar(res.message || 'تم حفظ النتائج النهائية ✓');
            if (res.data?.length) {
              this.processFinalGradesResponse(res.data, cls);
            }
          } else {
            this.showSnackbar(res.message || 'حدث خطأ');
          }
        },
        error: () => {
          this.saving.set(false);
          this.showSnackbar('حدث خطأ أثناء حفظ النتائج النهائية');
        }
      });
    }
  }

  autoFillAttendance() {
    const cls = this.currentCls();
    if (!cls) return;
    const periodId = this.currentPeriodId();
    if (!periodId) { this.showSnackbar('لم يتم العثور على معرف الفترة'); return; }
    this.api.autoFillAttendance(cls.id, periodId).subscribe({
      next: (res) => {
        this.showSnackbar(res.message || 'تم تعبئة درجات الغياب');
        this.clearGrades();
        this.loadEvaluations();
      },
      error: () => this.showSnackbar('فشل التعبئة التلقائية'),
    });
  }

  private getDateForDay(weekNum: number, dayName: string): string {
    const periodId = this.currentPeriodId();
    if (periodId) {
      const period = this.periods().find(p => p.id === periodId);
      if (period?.startDate) {
        const start = new Date(period.startDate);
        const dayMap: Record<string, number> = {
          'الأحد': 0, 'الاثنين': 1, 'الثلاثاء': 2, 'الأربعاء': 3,
          'الخميس': 4, 'الجمعة': 5, 'السبت': 6
        };
        const targetDay = dayMap[dayName] ?? 0;
        const currentDay = start.getDay();
        let diff = targetDay - currentDay;
        if (diff < 0) diff += 7; // next occurrence within the same week
        start.setDate(start.getDate() + diff);
        return start.toISOString().split('T')[0];
      }
    }
    const tmpl = this.currentTmpl();
    const num = Number(weekNum);
    if (!tmpl?.start_date) {
      const d = new Date();
      d.setDate(d.getDate() + (num - 1) * 7);
      return d.toISOString().split('T')[0];
    }
    const start = new Date(tmpl.start_date);
    start.setDate(start.getDate() + (num - 1) * 7);
    const dayMap: Record<string, number> = {
      'الأحد': 0, 'الاثنين': 1, 'الثلاثاء': 2, 'الأربعاء': 3,
      'الخميس': 4, 'الجمعة': 5, 'السبت': 6
    };
    const targetDay = dayMap[dayName] ?? 0;
    const currentDay = start.getDay();
    let diff = targetDay - currentDay;
    if (diff < 0) diff += 7;
    start.setDate(start.getDate() + diff);
    return start.toISOString().split('T')[0];
  }

  // ══════════════════════════════════════
  //  WORD EXPORT
  // ══════════════════════════════════════
  async exportWord() {
    const tmpl = this.exportTmpl();
    const cls = this.exportCls();
    if (!tmpl || !cls) return;

    this.exporting.set(true);
    const profile = this.schoolProfile();
    const schoolInfo = {
      directorate: tmpl.directorate || profile?.directorate || '',
      administration: tmpl.administration || profile?.educationalAdministration || '',
      school: tmpl.school || profile?.schoolName || '',
    };

    let exportPeriods = this.periods().filter(p => p.periodType === 'Weekly');
    if (tmpl.term != null) {
      exportPeriods = exportPeriods.filter(p => p.semesterNumber == null || p.semesterNumber === this.normalizeTerm(tmpl.term));
    }
    // Fetch periods from API if not yet loaded
    if (!exportPeriods.length) {
      try {
        const ayId = this.academicYearId();
        const periodsRes: any = await firstValueFrom(this.api.getPeriodsByAcademicYear(ayId));
        if (periodsRes?.isSuccess && periodsRes.data?.length) {
          this.periods.set(periodsRes.data);
          exportPeriods = periodsRes.data.filter((p: any) => p.periodType === 'Weekly');
          if (tmpl.term != null) {
            exportPeriods = exportPeriods.filter((p: any) => p.semesterNumber == null || p.semesterNumber === this.normalizeTerm(tmpl.term));
          }
        }
      } catch { }
    }

    // Build weeks — use exportPeriods if available, otherwise fall back to template week_names
    const wkCount = exportPeriods.length > 0
      ? Math.min(tmpl.weeks, exportPeriods.length)
      : (tmpl.weeks || 12);
    let weeks: { name: string; date: string }[] = [];
    if (exportPeriods.length > 0) {
      weeks = exportPeriods.slice(0, wkCount).map((p, i) => ({
        name: tmpl.week_names[i] || p.name || 'الأسبوع ' + (i + 1),
        date: p.startDate ? '(' + p.startDate + ')' : (tmpl.week_dates[i] || ''),
      }));
    } else {
      // Fallback: generate virtual week names from template data
      weeks = Array.from({ length: wkCount }, (_, i) => ({
        name: tmpl.week_names[i] || 'الأسبوع ' + (i + 1),
        date: tmpl.week_dates[i] ? '(' + tmpl.week_dates[i] + ')' : '',
      }));
    }

    const type = this.exportType();

    // Build extra header fields
    const gradeLevel = tmpl.stage || '';
    const termNum = this.normalizeTerm(tmpl.term);
    const termName = termNum === 1 ? 'الفصل الدراسي الأول'
      : termNum === 2 ? 'الفصل الدراسي الثاني'
      : 'الفصل الدراسي';
    const academicYearName = this.periods()[0]?.academicYearName || this.periods().find(p => p.academicYearName)?.academicYearName || '';

    try {
      let blob: Blob;

      if (type === 'weekly') {
        const gradeData: Record<string, any> = {};
        const allWeeks = await this.loadAllWeeksGrades(cls, tmpl);
        for (const st of cls.students) {
          for (let wn = 1; wn <= wkCount; wn++) {
            const id = `${st.id}_${wn}`;
            const wd = allWeeks[id];
            if (!wd) continue;
            const values: Record<string, number> = {};
            for (const c of tmpl.criteria) {
              values[c.id] = wd[c.id] ?? 0;
            }
            const total = Object.values(values).reduce((a, b) => a + b, 0);
            gradeData[`${wn}_${st.id}`] = { total, values };
          }
        }
        blob = await this.wg.generateWeeklyGrades({
          schoolInfo, title: tmpl.name, subject: cls.subject, classRoom: cls.name,
          gradeLevel, termName, academicYearName,
          students: cls.students, weeks, criteria: tmpl.criteria,
          gradeData, weeklyMax: tmpl.weekly_max,
        });

      } else if (type === 'attendance') {
        const absenceData: Record<string, any> = {};
        for (const st of cls.students) {
          for (let wn = 1; wn <= wkCount; wn++) {
            const days: Record<string, boolean> = {};
            let total = 0;
            for (const d of tmpl.absent_days) {
              const a = this.absenceValues()[st.id + '_' + wn + '_' + d];
              if (a) { days[d] = true; total++; }
            }
            absenceData[`${wn}_${st.id}`] = { total, days };
          }
        }
        blob = await this.wg.generateAttendanceSheet({
          schoolInfo, title: 'كشف الغياب الأسبوعي', subject: cls.subject, classRoom: cls.name,
          gradeLevel, termName, academicYearName,
          students: cls.students, weeks, days: tmpl.absent_days,
          absenceData, avgMax: 5,
        });

      } else {
        await firstValueFrom(this.api.recalculateFinalGrades(cls.id, this.entryTerm()));
        const [assessmentsRes, finalsRes] = await Promise.all([
          firstValueFrom(this.api.getAssessmentsByClass(cls.id, this.entryTerm())),
          firstValueFrom(this.api.getFinalGradesByClass(cls.id, this.entryTerm()))
        ]);

        const examData: Record<number, { exam1: number; exam2: number }> = {};
        if (assessmentsRes?.isSuccess && assessmentsRes.data) {
          for (const a of assessmentsRes.data) {
            const st = cls.students.find(s => (s.enrollmentId ?? s.id) === a.enrollmentId);
            if (!st) continue;
            if (!examData[st.id]) examData[st.id] = { exam1: 0, exam2: 0 };
            if (a.assessmentType === 'MonthlyExam1') examData[st.id].exam1 = a.score;
            else if (a.assessmentType === 'MonthlyExam2') examData[st.id].exam2 = a.score;
          }
        }

        const finalData: Record<number, { written_total: number; final_exam: number; total: number }> = {};
        if (finalsRes?.isSuccess && finalsRes.data) {
          for (const f of finalsRes.data) {
            const st = cls.students.find(s => (s.enrollmentId ?? s.id) === f.enrollmentId);
            if (!st) continue;
            finalData[st.id] = { written_total: f.writtenTotal ?? 0, final_exam: f.finalExamScore ?? 0, total: f.total ?? 0 };
          }
        }

        const gradeData: Record<string, any> = {};
        const allWeeks = await this.loadAllWeeksGrades(cls, tmpl);
        for (const st of cls.students) {
          for (let wn = 1; wn <= wkCount; wn++) {
            const id = `${st.id}_${wn}`;
            const wd = allWeeks[id];
            if (!wd) continue;
            const total = Object.values(tmpl.criteria).reduce((s, c) => s + (wd[c.id] ?? 0), 0);
            gradeData[`${wn}_${st.id}`] = { total };
          }
        }

        const monthAverages: Record<number, Record<string, number>> = {};
        for (const st of cls.students) {
          const avgs: Record<string, number> = {};
          for (const mg of tmpl.month_groups) {
            let sum = 0, count = 0;
            for (const wn of mg.weeks) {
              const g = gradeData[`${wn}_${st.id}`];
              if (g?.total != null) { sum += g.total; count++; }
            }
            if (count) avgs[mg.name] = Math.round(sum / count);
          }
          monthAverages[st.id] = avgs;
        }

        blob = await this.wg.generateSummaryGrades({
          schoolInfo, title: 'السجل المجمع', subject: cls.subject, classRoom: cls.name,
          gradeLevel, termName, academicYearName,
          students: cls.students, gradeColumns: tmpl.summary_columns.map(c => ({ id: c.id, label: c.name, max: c.max })),
          monthAverages, examData, finalData,
          monthGroups: tmpl.month_groups, gradeData,
        });
      }

      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${type}_${cls.name}.docx`;
      a.click();
      URL.revokeObjectURL(url);
      this.showSnackbar('تم التصدير بنجاح');
    } catch (e) {
      console.error(e);
      this.showSnackbar('فشل التصدير');
    }
    this.exporting.set(false);
  }

  private async loadAllWeeksGrades(cls: ClassItem, tmpl: Template): Promise<Record<string, Record<string, number>>> {
    const result: Record<string, Record<string, number>> = {};
    console.log('loadAllWeeksGrades: start', { classId: cls.id, studentsCount: cls.students.length, term: tmpl.term, weeks: tmpl.weeks });

    // 1) Try API: fetch periods then evaluations per period
    try {
      const ayId = this.academicYearId();
      const periodsRes: any = await firstValueFrom(this.api.getPeriodsByAcademicYear(ayId));
      console.log('loadAllWeeksGrades: periods API success', { isSuccess: periodsRes?.isSuccess, count: periodsRes?.data?.length });
      if (periodsRes?.isSuccess && periodsRes.data?.length) {
        this.periods.set(periodsRes.data);
        let periods = periodsRes.data.filter((p: any) => p.periodType === 'Weekly');
        console.log('loadAllWeeksGrades: after Weekly filter', { count: periods.length });
        if (tmpl.term != null) {
          periods = periods.filter((p: any) => p.semesterNumber == null || p.semesterNumber === this.normalizeTerm(tmpl.term));
          console.log('loadAllWeeksGrades: after term filter', { count: periods.length, term: tmpl.term });
        }
        const weeksCount = Math.min(tmpl.weeks, periods.length);
        console.log('loadAllWeeksGrades: weeksCount', weeksCount);
        for (let wn = 1; wn <= weeksCount; wn++) {
          const period = periods[wn - 1];
          if (!period) continue;
          try {
            console.log('loadAllWeeksGrades: fetching evaluations', { classId: cls.id, periodId: period.id, weekNum: wn });
            const res: any = await firstValueFrom(this.api.getEvaluationsByClassPeriod(cls.id, period.id));
            console.log('loadAllWeeksGrades: eval response', { isSuccess: res?.isSuccess, dataCount: res?.data?.length, periodId: period.id });
            if (res?.isSuccess && Array.isArray(res.data)) {
              for (const ce of res.data) {
                // Match by enrollmentId (more reliable than name)
                const st = cls.students.find(s => (s.enrollmentId ?? s.id) === ce.enrollmentId)
                  || cls.students.find(s => s.name === ce.studentName);
                if (!st) continue;
                const key = `${st.id}_${wn}`;
                if (!result[key]) result[key] = {};
                for (const ev of (ce.evaluations || [])) {
                  if (ev.score != null) result[key]['c' + ev.evaluationItemId] = ev.score;
                }
              }
            }
          } catch (e) {
            console.error(`loadAllWeeksGrades: period ${period.id} eval fetch failed`, e);
          }
        }
      }
    } catch (e) {
      console.error('loadAllWeeksGrades: periods fetch failed', e);
    }

    console.log('loadAllWeeksGrades: API result keys count', Object.keys(result).length);

    // 2) If API returned nothing, try local gradeValues (what user sees on screen)
    if (!Object.keys(result).length) {
      const gv = this.gradeValues();
      console.log('loadAllWeeksGrades: trying gradeValues fallback', { gradeValuesCount: Object.keys(gv).length });
      if (Object.keys(gv).length) {
        for (const st of cls.students) {
          for (let wn = 1; wn <= tmpl.weeks; wn++) {
            let hasData = false;
            const entry: Record<string, number> = {};
            for (const c of tmpl.criteria) {
              const val = gv[st.id + '_' + wn + '_' + c.id];
              if (val != null) { entry[c.id] = val; hasData = true; }
            }
            if (hasData) result[`${st.id}_${wn}`] = entry;
          }
        }
      } else {
        console.warn('loadAllWeeksGrades: no data from API and no local gradeValues');
      }
    }

    console.log('loadAllWeeksGrades: final result keys count', Object.keys(result).length);
    return result;
  }

  // ══════════════════════════════════════
  //  SNACKBAR
  // ══════════════════════════════════════
  private snackbarTimer: any = null;
  private showSnackbar(msg: string) {
    this.snackbar.set(msg);
    if (this.snackbarTimer) clearTimeout(this.snackbarTimer);
    this.snackbarTimer = setTimeout(() => this.snackbar.set(''), 3000);
  }
}
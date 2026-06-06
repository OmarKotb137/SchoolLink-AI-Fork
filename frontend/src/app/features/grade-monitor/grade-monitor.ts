import { Component, signal, computed, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, forkJoin, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { GradeMonitorService, Template, ClassItem, Student, SchoolProfile, EvaluationPeriod, Criteria } from './grade-monitor.service';
import { GradeLevelService, GradeLevel } from '../../core/services/grade-level.service';
import { WordGeneratorService } from './word-generator.service';

@Component({
  selector: 'app-grade-monitor',
  imports: [Sidebar, Topbar, FormsModule],
  templateUrl: './grade-monitor.html',
  styleUrl: './grade-monitor.css',
})
export class GradeMonitor implements OnInit {
  private api = inject(GradeMonitorService);
  private gradeLevelService = inject(GradeLevelService);
  private wg = inject(WordGeneratorService);
  private cdr = inject(ChangeDetectorRef);

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
  editTmplId = signal<number | null>(null);
  editClsId = signal<number | null>(null);
  private STORAGE_KEY = 'grade_monitor_classes';

  entryClassId = signal<number | null>(null);
  entryType = signal<'grades' | 'absence' | 'exams' | 'final'>('grades');
  entryWeek = signal<number>(1);
  enrollmentMap = signal<Record<number, number>>({}); // studentId → enrollmentId
  existingEvalMap = signal<Record<string, number>>({}); // key: enrollmentId_evalItemId → evalId

  // Local objects for two-way binding input values
  localGrades: Record<string, any> = {};
  localAbsences: Record<string, boolean> = {};
  localExams: Record<string, any> = {};
  localFinals: Record<string, any> = {};
  private absenceSnapshot: Record<string, boolean> = {};

  // ══ Loading indicator for week data ══
  loadingWeek = signal(false);
  dataReady = signal(false);
  pendingReload = signal(false);
  // إلغاء الطلبات القديمة عند استدعاء جديد
  private cancelLoad$ = new Subject<void>();

  currentPeriodId = computed(() => {
    const weekNum = Number(this.entryWeek());
    const p = this.periods().find(p => p.periodType === 1 && p.orderNum === weekNum);
    return p?.id ?? null;
  });

  exportClassId = signal<number | null>(null);
  exportType = signal<'weekly' | 'attendance' | 'summary'>('weekly');
  exporting = signal(false);

  // Loading / status
  saving = signal(false);
  snackbar = signal('');

  // ══════════════════════════════════════
  //  INIT
  // ══════════════════════════════════════
  ngOnInit() {
    this.loadFromLocalStorage();
    this.loadGrades();
    this.loadApiClasses();

    Promise.all([
      new Promise<void>(resolve => {
        this.api.getSchoolProfile().subscribe({
          next: (res) => { if (res.isSuccess) this.schoolProfile.set(res.data); resolve(); },
          error: () => resolve(),
        });
      }),
      new Promise<void>(resolve => {
        this.api.getPeriodsByAcademicYear(1).subscribe(res => {
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
      this.loadTemplatesAsync(),
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
  tmplModalOpen = signal(false);

  private async loadTemplatesAsync() {
    const res = await firstValueFrom(this.api.getTemplatesByGradeLevel(1, 1));
    if (!res.isSuccess) return;
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
    const weeks = this.periods().filter((p: any) => p.periodType === 1);
    const wNames = weeks.map((w: any, i: number) => w.name || weekNames[i] || '');
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
      this.api.updateTemplate(apiId, { id: apiId, name: data.name, calculationType: 1, isActive: true }).subscribe({
        next: () => saveItems(apiId),
        error: () => saveLocal(),
      });
    } else {
      if (!this.tSubjectId()) { this.showSnackbar('اختر المادة أولاً'); this.saving.set(false); return; }
      this.api.createTemplate({ gradeLevelId: 1, subjectId: this.tSubjectId(), academicYearId: 1, name: data.name, calculationType: 1 }).subscribe({
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
        const sortedGrades = data.sort((a, b) => a.levelOrder - b.levelOrder);
        this.grades.set(sortedGrades);
      }
    });
  }

  private loadApiClasses() {
    this.api.getClasses().subscribe({
      next: (res) => {
        if (res.isSuccess) this.apiClasses.set(res.data || []);
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
          if (res.isSuccess && res.data?.length) {
            this.classes.set((res.data as any[]).map((link: any) => ({
              linkId: link.id,
              id: link.classId,
              name: link.className || '',
              teacher: link.teacher || '',
              subject: link.subject || '',
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

  // Computed: classes NOT yet linked (have no template chosen or came fresh from API)
  unlinkedApiClasses = computed(() => this.classes());

  filteredApiClasses = computed(() => {
    const gradeId = this.linkedApiGradeId();
    if (!gradeId) return [];
    return this.apiClasses().filter(c => c.gradeLevelId === gradeId);
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
    this.cTeacher.set(found?.teacher ?? '');
    this.cSubj.set(found?.subject ?? '');
    this.cYear.set(found?.year ?? c?.year ?? '2025/2026');
    this.cStudents.set(
      (found?.students ?? c?.students ?? []).map((s: any) => s.name).join('\n')
    );
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
      this.cTeacher.set(found.teacher || '');
      this.cSubj.set(found.subject || '');
      this.cYear.set(found.year || '');
      this.cStudents.set((found.students || []).map((s: any) => s.name).join('\n'));
      this.cTmplId.set(null);
    }
  }

  saveCls() {
    if (this.cTmplId() === null) return;

    const apiClsId = this.linkedApiClsId();
    if (apiClsId == null) return;
    this.api.createLink({ classId: Number(apiClsId), templateId: this.cTmplId()!, academicYearId: 1 }).subscribe({
      next: () => {
        this.loadLinksFromApi();
        this.clsModalOpen.set(false);
        this.showSnackbar('تم ربط الفصل بالقالب');
      },
      error: () => this.showSnackbar('فشل الربط'),
    });
  }

  delCls(id: number) {
    const item = this.classes().find(c => c.id === id);
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
    const cid = this.entryClassId();
    if (!cid) return null;
    const cls = this.classes().find(c => c.id === Number(cid));
    if (!cls) return null;
    return this.templates().find(t => t.id === cls.template_id)
      || (cls.subject ? this.templates().find(t => t.subjectName === cls.subject) : null)
      || null;
  });

  currentCls = computed(() => {
    const cid = this.entryClassId();
    if (!cid) return null;
    return this.classes().find(c => c.id === Number(cid)) || null;
  });

  weeklyMax = computed(() => {
    const tmpl = this.currentTmpl();
    return tmpl ? tmpl.criteria.reduce((a, c) => a + c.max, 0) : 40;
  });

  stats = computed(() => ({
    classes: this.classes().length,
    students: this.classes().reduce((a, c) => a + c.students.length, 0),
    entries: 0,
    templates: this.templates().length,
  }));

  recentClasses = computed(() => this.classes().slice(0, 5));

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
    this.activeSection.set('entry');
    this.clearGrades();
    if (this.dataReady()) this.loadEvaluations();
    else this.pendingReload.set(true);
  }

  // ══ يُستدعى من الـ HTML عند تغيير الأسبوع أو الفصل ══
  onClassOrWeekChange() {
    // إلغاء أي طلب HTTP جاري
    this.cancelLoad$.next();
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
  entryWeeks = computed(() => {
    const tmpl = this.currentTmpl();
    if (!tmpl) return [];
    const periods = this.periods().filter(p => p.periodType === 1);
    const maxWeeks = Math.min(tmpl.weeks, periods.length);
    return Array.from({ length: maxWeeks }, (_, i) => i + 1);
  });

  weekLabel(wn: number): string {
    const tmpl = this.currentTmpl();
    if (!tmpl) return 'الأسبوع ' + wn;
    const num = Number(wn);
    const wName = tmpl.week_names[num - 1] || num;
    const wDate = tmpl.week_dates[num - 1] ? ' (' + tmpl.week_dates[num - 1] + ')' : '';
    return 'الأسبوع ' + wName + wDate;
  }

  private getPeriodId(weekNum: number): number | null {
    const num = Number(weekNum);
    const period = this.periods().find(p => p.periodType === 1 && p.orderNum === num);
    return period?.id ?? null;
  }

  exportTmpl = computed(() => {
    const cid = this.exportClassId();
    if (!cid) return null;
    const cls = this.classes().find(c => c.id === Number(cid));
    if (!cls) return null;
    return this.templates().find(t => t.id === cls.template_id)
      || (cls.subject ? this.templates().find(t => t.subjectName === cls.subject) : null)
      || null;
  });

  exportCls = computed(() => {
    const cid = this.exportClassId();
    if (!cid) return null;
    return this.classes().find(c => c.id === Number(cid)) || null;
  });

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
    return tmpl.criteria.reduce((sum, c) =>
      sum + (this.gradeValues()[sid + '_' + wn + '_' + c.id] || 0), 0);
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
    return this.getFinal(sid, 'ma') + this.getFinal(sid, 'e1') + this.getFinal(sid, 'e2');
  }

  finalTotal(sid: number): number {
    return this.finalWritten(sid) + this.getFinal(sid, 'fe');
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
    if (!this.dataReady()) { this.pendingReload.set(true); return; }
    const cls = this.currentCls();
    const tmpl = this.currentTmpl();
    const wn = Number(this.entryWeek());
    if (!cls || !tmpl) return;

    // ① إلغاء أي طلب HTTP سابق (race condition fix)
    this.cancelLoad$.next();

    // ② امسح الداتا القديمة
    this.gradeValues.set({});
    this.absenceValues.set({});
    this.enrollmentMap.set({});
    this.existingEvalMap.set({});
    this.localGrades = {};
    this.localAbsences = {};
    this.localExams = {};
    this.localFinals = {};
    this.absenceSnapshot = {};

    const periodId = this.currentPeriodId();
    if (!periodId) return;

    // بناء set من الـ item ids للـ template الحالي فقط
    const tmplItemIds = new Set(
      tmpl.criteria
        .map(c => parseInt(c.id.replace('c', '')))
        .filter(n => !isNaN(n))
    );

    this.loadingWeek.set(true);

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
              // ③ فقط الـ items التابعة للـ template الحالي
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
    const firstDay = this.getDateForDay(wn, tmpl.absent_days[0]);
    const lastDay = this.getDateForDay(wn, tmpl.absent_days[tmpl.absent_days.length - 1]);
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

  onExamChange(sid: number, examNum: number, val: any) {
    const key = sid + '_e' + examNum;
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

  onFinalChange(sid: number, field: string, val: any) {
    const key = sid + '_' + field;
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
    if (!periodId) {
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

    } else {
      // exams / final — لا يحتاج API خاص حالياً
      this.saving.set(false);
      this.showSnackbar('تم الحفظ محلياً');
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
    const diff = targetDay - currentDay;
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

    const wkCount = Math.min(tmpl.weeks, this.periods().filter(p => p.periodType === 1).length);
    const weeks = tmpl.week_names.slice(0, wkCount).map((name, i) => ({
      name,
      date: tmpl.week_dates[i] || '',
    })).filter(w => w.name);

    const type = this.exportType();

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
          students: cls.students, weeks, days: tmpl.absent_days,
          absenceData, avgMax: 5,
        });

      } else {
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
        blob = await this.wg.generateSummaryGrades({
          schoolInfo, title: 'السجل المجمع', subject: cls.subject, classRoom: cls.name,
          students: cls.students, gradeColumns: tmpl.summary_columns.map(c => ({ id: c.id, label: c.name, max: c.max })),
          monthAverages: {}, examData: {}, finalData: {},
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
    const periods = this.periods().filter(p => p.periodType === 1);
    const weeksCount = Math.min(tmpl.weeks, periods.length);
    for (let wn = 1; wn <= weeksCount; wn++) {
      const period = periods.find(p => p.orderNum === wn);
      if (!period) continue;
      try {
        const res: any = await firstValueFrom(this.api.getEvaluationsByClassPeriod(cls.id, period.id));
        if (!res?.isSuccess || !res.data?.length) continue;
        for (const ce of res.data) {
          const st = cls.students.find(s => s.name === ce.studentName)
            || cls.students.find(s => (s.enrollmentId ?? s.id) === ce.enrollmentId);
          if (!st) continue;
          const key = `${st.id}_${wn}`;
          if (!result[key]) result[key] = {};
          for (const ev of (ce.evaluations || [])) {
            if (ev.score != null) result[key]['c' + ev.evaluationItemId] = ev.score;
          }
        }
      } catch { }
    }
    return result;
  }

  // ══════════════════════════════════════
  //  SNACKBAR
  // ══════════════════════════════════════
  private showSnackbar(msg: string) {
    this.snackbar.set(msg);
    setTimeout(() => this.snackbar.set(''), 3000);
  }
}
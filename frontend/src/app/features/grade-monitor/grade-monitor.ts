import { Component, signal, computed, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { GradeMonitorService, Template, ClassItem, Student, SchoolProfile, EvaluationPeriod } from './grade-monitor.service';
import { WordGeneratorService } from './word-generator.service';

@Component({
  selector: 'app-grade-monitor',
  imports: [Sidebar, Topbar, FormsModule],
  templateUrl: './grade-monitor.html',
  styleUrl: './grade-monitor.css',
})
export class GradeMonitor implements OnInit {
  private api = inject(GradeMonitorService);
  private wg = inject(WordGeneratorService);
  private cdr = inject(ChangeDetectorRef);

  sidebarOpen = signal(false);
  activeSection = signal<'dash' | 'tmpl' | 'cls' | 'entry' | 'exp'>('dash');

  // ══════════════════════════════════════
  //  SIGNALS
  // ══════════════════════════════════════
  templates = signal<Template[]>([]);
  classes = signal<ClassItem[]>([]);
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

  // ══ Loading indicator for week data ══
  loadingWeek = signal(false);

  currentPeriodId = computed(() => {
    const p = this.periods().find(p => p.periodType === 1 && p.orderNum === this.entryWeek());
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
    this.loadClassesFromApi();
    this.api.getSchoolProfile().subscribe({
      next: (res) => {
        if (res.isSuccess) this.schoolProfile.set(res.data);
      },
      error: () => {},
    });
    this.api.getPeriodsByAcademicYear(1).subscribe(res => {
      if (res.isSuccess) this.periods.set(res.data || []);
    });
    this.api.getSubjects().subscribe(res => {
      if (res.isSuccess) this.subjects.set(res.data || []);
    });
    this.loadTemplates();
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
  tCriteria = signal<{ id: string; name: string; max: number }[]>([
    { id: 'default_1', name: 'الأداء المنزلي', max: 10 },
    { id: 'default_2', name: 'التقييم الأسبوعي', max: 20 },
    { id: 'default_3', name: 'السلوك والمواظبة', max: 10 },
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
  tmplModalOpen = signal(false);

  loadTemplates() {
    this.api.getTemplatesByGradeLevel(1, 1).subscribe(res => {
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
      let done = 0;
      for (const t of rawList) {
        this.api.getItemsByTemplate(t.id).subscribe({
          next: (itemRes) => {
            const items = itemRes.isSuccess ? (itemRes.data as any[]) || [] : [];
            const criteria = items
              .filter((i: any) => i.isVisible !== false)
              .sort((a: any, b: any) => a.displayOrder - b.displayOrder)
              .map((i: any) => ({ id: 'c' + i.id, name: i.name, max: i.maxScore }));
            const weeklyMax = criteria.reduce((a: number, c: any) => a + c.max, 0);
            this.templates.update(list => [...list, this.toFrontendTemplate(t, criteria, weeklyMax)]);
          },
          error: () => {
            this.templates.update(list => [...list, this.toFrontendTemplate(t, [], 0)]);
          },
          complete: () => { done++; if (done === rawList.length) this.saveToLocalStorage(); }
        });
      }
    });
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
    return {
      id: t.id,
      apiId: t.id,
      name: t.name,
      stage: t.gradeLevelName || '',
      subjectId: t.subjectId,
      weeks: weeks.length || 12,
      start_date: weeks[0]?.startDate || '',
      school: p?.schoolName || '',
      directorate: p?.directorate || '',
      administration: p?.educationalAdministration || '',
      criteria,
      summary_columns: [
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
      absent_days: ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس'],
      weekly_max: weeklyMax,
      month_groups: [
        { name: 'فبراير', weeks: [1, 2, 3, 4] },
        { name: 'مارس', weeks: [5, 6, 7, 8] },
        { name: 'أبريل', weeks: [9, 10, 11, 12] },
      ],
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

    const criteria = t?.criteria?.length ? t.criteria : [
      { id: 'default_1', name: 'الأداء المنزلي', max: 10 },
      { id: 'default_2', name: 'التقييم الأسبوعي', max: 20 },
      { id: 'default_3', name: 'السلوك والمواظبة', max: 10 },
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
      absent_days: ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس'],
      weekly_max: this.tCriteria().reduce((a, c) => a + c.max, 0),
      month_groups: [
        { name: 'فبراير', weeks: [1, 2, 3, 4] },
        { name: 'مارس', weeks: [5, 6, 7, 8] },
        { name: 'أبريل', weeks: [9, 10, 11, 12] },
        { name: 'مايو', weeks: [13, 14] },
      ],
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
          const body = {
            templateId: tmplApiId,
            name: c.name,
            maxScore: c.max,
            weight: 1,
            itemType: 1,
            autoCalcType: 0,
            displayOrder: idx + 1,
            isVisible: true
          };
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

  addCrit() {
    this.tCriteria.update(list => {
      const newIdx = list.filter(c => c.id.startsWith('new_')).length + 1;
      return [...list, { id: 'new_' + newIdx, name: 'عنصر ' + newIdx, max: 10 }];
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
  //  PERSISTENCE (localStorage)
  // ══════════════════════════════════════
  private saveToLocalStorage() {
    try {
      const data = { classes: this.classes(), templates: this.templates() };
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(data));
    } catch {}
  }

  private loadRawFromLocalStorage(): { classes?: any[]; templates?: any[] } | null {
    try {
      const raw = localStorage.getItem(this.STORAGE_KEY);
      if (!raw) return null;
      return JSON.parse(raw);
    } catch { return null; }
  }

  private loadFromLocalStorage() {
    const data = this.loadRawFromLocalStorage();
    if (!data) return;
    if (data.classes?.length) this.classes.set(data.classes);
    if (data.templates?.length) this.templates.set(data.templates);
  }

  private loadClassesFromApi() {
    this.api.getClasses().subscribe({
      next: (res) => {
        if (res.isSuccess && res.data?.length) {
          this.classes.set(res.data);
          this.saveToLocalStorage();
        }
      },
      error: () => {},
    });
  }

  // ══════════════════════════════════════
  //  CLASSES
  // ══════════════════════════════════════
  cName = signal('');
  cTeacher = signal('');
  cSubj = signal('');
  cYear = signal('2025/2026');
  cStudents = signal('');
  cTmplId = signal<number | null>(null);
  clsModalOpen = signal(false);

  openClsForm(c?: ClassItem) {
    this.editClsId.set(c ? c.id : null);
    this.cName.set(c?.name || '');
    this.cTeacher.set(c?.teacher || '');
    this.cSubj.set(c?.subject || '');
    this.cYear.set(c?.year || '2025/2026');
    this.cStudents.set((c?.students || []).map(s => s.name).join('\n'));
    this.cTmplId.set(c?.template_id || null);
    this.clsModalOpen.set(true);
  }

  saveCls() {
    if (!this.cName().trim() || this.cTmplId() === null) return;
    const studentNames = this.cStudents().split('\n')
      .map(s => s.trim()).filter(Boolean);
    const students: Student[] = studentNames.map((name, i) => ({ id: i + 1, name }));
    const data: ClassItem = {
      id: this.editClsId() || Date.now(),
      name: this.cName().trim(),
      teacher: this.cTeacher().trim(),
      subject: this.cSubj().trim(),
      year: this.cYear().trim(),
      template_id: this.cTmplId()!,
      students,
    };
    if (this.editClsId()) {
      this.classes.update(list => list.map(c => c.id === data.id ? data : c));
    } else {
      this.classes.update(list => [...list, data]);
    }
    this.saveToLocalStorage();
    this.clsModalOpen.set(false);
    this.showSnackbar(this.editClsId() ? 'تم تحديث الفصل' : 'تم إنشاء الفصل');
    this.api.createClass({
      name: data.name, teacher: data.teacher, subject: data.subject,
      year: data.year, templateId: data.template_id, students: studentNames,
    }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.classes.update(list => list.map(c => c.id === data.id ? {
            id: res.data.id, name: res.data.name, teacher: res.data.teacher,
            subject: res.data.subject, year: res.data.year,
            template_id: res.data.template_id ?? data.template_id,
            students: (res.data.students || []).map((s: any) => ({
              id: s.id, name: s.name, enrollmentId: s.enrollmentId
            })),
          } : c));
          this.saveToLocalStorage();
        }
      },
      error: () => {},
    });
  }

  delCls(id: number) {
    this.classes.update(list => list.filter(c => c.id !== id));
    this.saveToLocalStorage();
    this.api.deleteClass(id).subscribe({ error: () => {} });
  }

  // ══════════════════════════════════════
  //  COMPUTED
  // ══════════════════════════════════════
  currentTmpl = computed(() => {
    const cid = this.entryClassId();
    if (!cid) return null;
    const cls = this.classes().find(c => c.id === cid);
    if (!cls) return null;
    return this.templates().find(t => t.id === cls.template_id) || null;
  });

  currentCls = computed(() => {
    const cid = this.entryClassId();
    if (!cid) return null;
    return this.classes().find(c => c.id === cid) || null;
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
    this.entryClassId.set(cid);
    this.entryWeek.set(1);
    this.activeSection.set('entry');
    this.onClassOrWeekChange();
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
    const wName = tmpl.week_names[wn - 1] || wn;
    const wDate = tmpl.week_dates[wn - 1] ? ' (' + tmpl.week_dates[wn - 1] + ')' : '';
    return 'الأسبوع ' + wName + wDate;
  }

  private getPeriodId(weekNum: number): number | null {
    const period = this.periods().find(p => p.periodType === 1 && p.orderNum === weekNum);
    return period?.id ?? null;
  }

  exportTmpl = computed(() => {
    const cid = this.exportClassId();
    if (!cid) return null;
    const cls = this.classes().find(c => c.id === cid);
    if (!cls) return null;
    return this.templates().find(t => t.id === cls.template_id) || null;
  });

  exportCls = computed(() => {
    const cid = this.exportClassId();
    if (!cid) return null;
    return this.classes().find(c => c.id === cid) || null;
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

  setGrade(sid: number, cid: string, val: number | string) {
    const wn = this.entryWeek();
    const numVal = typeof val === 'string' ? parseFloat(val) : val;
    if (isNaN(numVal)) return;
    this.gradeValues.update(m => ({ ...m, [sid + '_' + wn + '_' + cid]: numVal }));
  }

  gradeTotal(sid: number): number {
    const wn = this.entryWeek();
    const tmpl = this.currentTmpl();
    if (!tmpl) return 0;
    return tmpl.criteria.reduce((sum, c) =>
      sum + (this.gradeValues()[sid + '_' + wn + '_' + c.id] || 0), 0);
  }

  toggleAbs(sid: number, day: string) {
    const wn = this.entryWeek();
    const key = sid + '_' + wn + '_' + day;
    this.absenceValues.update(m => ({ ...m, [key]: !m[key] }));
  }

  isAbsent(sid: number, day: string): boolean {
    const wn = this.entryWeek();
    return !!this.absenceValues()[sid + '_' + wn + '_' + day];
  }

  absenceCount(sid: number): number {
    const tmpl = this.currentTmpl();
    if (!tmpl) return 0;
    const wn = this.entryWeek();
    return tmpl.absent_days.filter(d => this.isAbsent(sid, d)).length;
  }

  setExam(sid: number, examNum: number, val: number | string) {
    const numVal = typeof val === 'string' ? parseFloat(val) : val;
    if (isNaN(numVal)) return;
    this.examValues.update(m => ({ ...m, [sid + '_e' + examNum]: numVal }));
  }

  getExam(sid: number, examNum: number): number {
    return this.examValues()[sid + '_e' + examNum] || 0;
  }

  setFinal(sid: number, field: string, val: number | string) {
    const numVal = typeof val === 'string' ? parseFloat(val) : val;
    if (isNaN(numVal)) return;
    this.finalValues.update(m => ({ ...m, [sid + '_' + field]: numVal }));
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
    const cls = this.currentCls();
    const tmpl = this.currentTmpl();
    const wn = this.entryWeek();
    if (!cls || !tmpl) return;

    // امسح القيم الحالية فوراً
    this.gradeValues.set({});
    this.absenceValues.set({});
    this.enrollmentMap.set({});
    this.existingEvalMap.set({});

    const periodId = this.currentPeriodId();
    if (!periodId) return;

    this.loadingWeek.set(true);

    this.api.getEvaluationsByClassPeriod(cls.id, periodId).subscribe({
      next: (res) => {
        this.loadingWeek.set(false);
        if (!res.isSuccess || !res.data?.length) return;

        const classEvals = res.data as any[];
        const grades: Record<string, number> = {};
        const evalIds: Record<string, number> = {};
        const enrollMap: Record<number, number> = {};

        for (const ce of classEvals) {
          // ابحث عن الطالب بالاسم أولاً، ثم بالـ enrollmentId
          const st = cls.students.find(s => s.name === ce.studentName)
            || cls.students.find(s => (s.enrollmentId ?? s.id) === ce.enrollmentId);
          const eid = ce.enrollmentId;

          if (st) enrollMap[st.id] = eid;

          for (const ev of (ce.evaluations || [])) {
            if (ev.score == null) continue;
            const studentId = st?.id ?? eid;
            const key = studentId + '_' + wn + '_c' + ev.evaluationItemId;
            grades[key] = ev.score;
            // evalIds: key = enrollmentId_evaluationItemId → evalId (لتحديد create/update)
            evalIds[eid + '_' + ev.evaluationItemId] = ev.id;
          }
        }

        if (Object.keys(grades).length) this.gradeValues.set(grades);
        if (Object.keys(evalIds).length) this.existingEvalMap.set(evalIds);
        if (Object.keys(enrollMap).length) this.enrollmentMap.set(enrollMap);
      },
      error: () => {
        this.loadingWeek.set(false);
      }
    });
  }

  // ══ يُستدعى من الـ HTML عند تغيير الفصل أو الأسبوع ══
  onClassOrWeekChange() {
    this.clearGrades();
    this.loadEvaluations();
  }

  clearGrades() {
    this.gradeValues.set({});
    this.absenceValues.set({});
    this.examValues.set({});
    this.finalValues.set({});
    this.enrollmentMap.set({});
    this.existingEvalMap.set({});
  }

  onGradeInput(sid: number, cid: string, event: Event) {
    const val = (event.target as HTMLInputElement).value;
    if (val === '') return;
    const numVal = parseFloat(val);
    if (!isNaN(numVal)) this.setGrade(sid, cid, numVal);
  }

  onExamInput(sid: number, examNum: number, event: Event) {
    const val = (event.target as HTMLInputElement).value;
    if (val === '') return;
    const numVal = parseFloat(val);
    if (!isNaN(numVal)) this.setExam(sid, examNum, numVal);
  }

  onFinalInput(sid: number, field: string, event: Event) {
    const val = (event.target as HTMLInputElement).value;
    if (val === '') return;
    const numVal = parseFloat(val);
    if (!isNaN(numVal)) this.setFinal(sid, field, numVal);
  }

  getGradeDisplay(sid: number, cid: string): string {
    const v = this.gradeValues()[sid + '_' + this.entryWeek() + '_' + cid];
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
    const wn = this.entryWeek();
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

      const requests: any[] = [];

      for (const st of students) {
        const enrollmentId = st.enrollmentId ?? enrollMap[st.id] ?? st.id;
        for (const c of tmpl.criteria) {
          const score = this.gradeValues()[st.id + '_' + wn + '_' + c.id];
          if (score == null) continue;

          const evalItemId = parseInt(c.id.replace('c', ''));
          if (isNaN(evalItemId)) continue;

          const existingId = evalMap[enrollmentId + '_' + evalItemId];
          if (existingId) {
            // تحديث درجة موجودة
            requests.push(
              this.api.updateEvaluation({ evaluationId: existingId, newScore: score })
            );
          } else {
            // إنشاء درجة جديدة
            requests.push(
              this.api.recordEvaluation({
                enrollmentId,
                evaluationItemId: evalItemId,
                periodId,
                score,
              })
            );
          }
        }
      }

      if (requests.length) {
        forkJoin(requests).subscribe({
          next: () => {
            this.saving.set(false);
            this.showSnackbar('تم حفظ الدرجات بنجاح ✓');
            // أعد تحميل البيانات لتحديث existingEvalMap
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

      for (const st of students) {
        const enrollmentId = st.enrollmentId ?? enrollMap[st.id] ?? st.id;
        for (const day of tmpl.absent_days) {
          const isAbsent = this.absenceValues()[st.id + '_' + wn + '_' + day];
          if (isAbsent == null) continue;
          requests.push(
            this.api.recordAbsence({
              enrollmentId,
              absenceDate: this.getDateForDay(wn, day),
              isAbsent,
              periodId,
            })
          );
        }
      }

      if (requests.length) {
        forkJoin(requests).subscribe({
          next: () => {
            this.saving.set(false);
            this.showSnackbar('تم حفظ الغياب بنجاح ✓');
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
    if (!tmpl?.start_date) {
      const d = new Date();
      d.setDate(d.getDate() + (weekNum - 1) * 7);
      return d.toISOString().split('T')[0];
    }
    const start = new Date(tmpl.start_date);
    start.setDate(start.getDate() + (weekNum - 1) * 7);
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
            const values: Record<string, number> = {};
            for (const c of tmpl.criteria) {
              values[c.id] = wd?.[c.id] ?? 0;
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
            const total = Object.values(tmpl.criteria).reduce((s, c) => s + (wd?.[c.id] ?? 0), 0);
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
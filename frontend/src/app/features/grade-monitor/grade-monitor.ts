import { Component, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

interface Criteria {
  id: string;
  name: string;
  max: number;
}

interface SumColumn {
  id: string;
  name: string;
  max: number;
}

interface Template {
  id: number;
  name: string;
  stage: string;
  weeks: number;
  start_date: string;
  school: string;
  directorate: string;
  administration: string;
  criteria: Criteria[];
  summary_columns: SumColumn[];
  week_names: string[];
  week_dates: string[];
  absent_days: string[];
  weekly_max: number;
  month_groups: { name: string; weeks: number[] }[];
}

interface Student {
  id: number;
  name: string;
}

interface ClassItem {
  id: number;
  name: string;
  teacher: string;
  subject: string;
  year: string;
  template_id: number;
  students: Student[];
}

@Component({
  selector: 'app-grade-monitor',
  imports: [Sidebar, Topbar, FormsModule],
  templateUrl: './grade-monitor.html',
  styleUrl: './grade-monitor.css',
})
export class GradeMonitor {
  sidebarOpen = signal(false);
  activeSection = signal<'dash' | 'tmpl' | 'cls' | 'entry' | 'exp'>('dash');

  templates = signal<Template[]>([]);
  classes = signal<ClassItem[]>([]);
  editTmplId = signal<number | null>(null);
  editClsId = signal<number | null>(null);

  entryClassId = signal<number | null>(null);
  entryType = signal<'grades' | 'absence' | 'exams' | 'final'>('grades');
  entryWeek = signal<number>(1);

  exportClassId = signal<number | null>(null);
  exportType = signal<'weekly' | 'attendance' | 'summary'>('weekly');

  // Template form
  tName = signal('');
  tStage = signal('الصف الأول الإعدادي');
  tWeeks = signal(14);
  tStart = signal('');
  tSchool = signal('الاعدادية الحديثة المشتركة بالمنشاة الكبرى');
  tDir = signal('مديرية التربية والتعليم بأسيوط');
  tAdm = signal('ادارة القوصية التعليمية');
  tCriteria = signal<Criteria[]>([
    { id: 'c1', name: 'الأداء المنزلي', max: 10 },
    { id: 'c2', name: 'التقييم الأسبوعي', max: 20 },
    { id: 'c3', name: 'السلوك والمواظبة', max: 10 },
  ]);
  tSumCols = signal<SumColumn[]>([
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

  // Class form
  cName = signal('');
  cTeacher = signal('');
  cSubj = signal('');
  cYear = signal('2025/2026');
  cStudents = signal('');
  cTmplId = signal<number | null>(null);

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

  tmplModalOpen = signal(false);
  clsModalOpen = signal(false);

  now = new Date();

  stats = computed(() => ({
    classes: this.classes().length,
    students: this.classes().reduce((a, c) => a + c.students.length, 0),
    entries: 0,
    templates: this.templates().length,
  }));

  recentClasses = computed(() => this.classes().slice(0, 5));

  sections = ['dash','tmpl','cls','entry','exp'] as const;

  setSection(s: string) {
    if (this.sections.includes(s as any)) {
      this.activeSection.set(s as any);
    }
  }

  openTmplForm(t?: Template) {
    this.editTmplId.set(t ? t.id : null);
    this.tName.set(t?.name || '');
    this.tStage.set(t?.stage || 'الصف الأول الإعدادي');
    this.tWeeks.set(t?.weeks || 14);
    this.tStart.set(t?.start_date || '');
    this.tSchool.set(t?.school || 'الاعدادية الحديثة المشتركة بالمنشاة الكبرى');
    this.tDir.set(t?.directorate || 'مديرية التربية والتعليم بأسيوط');
    this.tAdm.set(t?.administration || 'ادارة القوصية التعليمية');
    this.tCriteria.set(t?.criteria || [
      { id: 'c1', name: 'الأداء المنزلي', max: 10 },
      { id: 'c2', name: 'التقييم الأسبوعي', max: 20 },
      { id: 'c3', name: 'السلوك والمواظبة', max: 10 },
    ]);
    this.tSumCols.set(t?.summary_columns || [
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
    this.tmplModalOpen.set(true);
  }

  saveTmpl() {
    if (!this.tName().trim()) return;
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
      weeks: this.tWeeks(),
      start_date: this.tStart(),
      school: this.tSchool(),
      directorate: this.tDir(),
      administration: this.tAdm(),
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
    if (this.editTmplId()) {
      this.templates.update(list => list.map(t => t.id === data.id ? data : t));
    } else {
      this.templates.update(list => [...list, data]);
    }
    this.tmplModalOpen.set(false);
  }

  delTmpl(id: number) {
    this.templates.update(list => list.filter(t => t.id !== id));
  }

  addCrit() {
    this.tCriteria.update(list => {
      const nid = 'c' + (list.length + 1);
      return [...list, { id: nid, name: '', max: 10 }];
    });
  }

  removeCrit(idx: number) {
    this.tCriteria.update(list => list.filter((_, i) => i !== idx));
  }

  addSumCol() {
    this.tSumCols.update(list => [
      ...list,
      { id: 'col_' + (list.length + 1), name: '', max: 40 },
    ]);
  }

  removeSumCol(idx: number) {
    this.tSumCols.update(list => list.filter((_, i) => i !== idx));
  }

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
    const nextId = this.classes().length ? Math.max(...this.classes().map(c => c.id)) + 1 : 1;
    const students: Student[] = this.cStudents().split('\n')
      .map(s => s.trim()).filter(Boolean)
      .map((name, i) => ({ id: i + 1, name }));
    const data: ClassItem = {
      id: this.editClsId() || nextId,
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
    this.clsModalOpen.set(false);
  }

  delCls(id: number) {
    this.classes.update(list => list.filter(c => c.id !== id));
  }

  goEntry(cid: number) {
    this.entryClassId.set(cid);
    this.entryWeek.set(1);
    this.activeSection.set('entry');
  }

  goExp(cid: number) {
    this.exportClassId.set(cid);
    this.activeSection.set('exp');
  }

  // Entry helpers
  entryWeeks = computed(() => {
    const tmpl = this.currentTmpl();
    if (!tmpl) return [];
    return Array.from({ length: tmpl.weeks }, (_, i) => i + 1);
  });

  entryWeekLabel = computed(() => {
    const tmpl = this.currentTmpl();
    const wn = this.entryWeek();
    if (!tmpl) return 'الأسبوع ' + wn;
    const wName = tmpl.week_names[wn - 1] || wn;
    const wDate = tmpl.week_dates[wn - 1] ? ' (' + tmpl.week_dates[wn - 1] + ')' : '';
    return 'الأسبوع ' + wName + wDate;
  });

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

  // Grade input map: "studentId_weekNum_criteriaId" -> number
  gradeValues = signal<Record<string, number>>({});

  // Absence map: "studentId_weekNum_dayName" -> boolean (true = absent)
  absenceValues = signal<Record<string, boolean>>({});

  // Exam values: "studentId_examNum" -> number
  examValues = signal<Record<string, number>>({});

  // Final values
  finalValues = signal<Record<string, number>>({});

  getStudents() {
    return this.currentCls()?.students || [];
  }

  setGrade(sid: number, cid: string, val: number) {
    const wn = this.entryWeek();
    this.gradeValues.update(m => ({ ...m, [sid + '_' + wn + '_' + cid]: val }));
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

  setExam(sid: number, examNum: number, val: number) {
    this.examValues.update(m => ({ ...m, [sid + '_e' + examNum]: val }));
  }

  getExam(sid: number, examNum: number): number {
    return this.examValues()[sid + '_e' + examNum] || 0;
  }

  setFinal(sid: number, field: string, val: number) {
    this.finalValues.update(m => ({ ...m, [sid + '_' + field]: val }));
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
      const filled = vals.length;
      const students = this.getStudents().length;
      const total = vals.reduce((a, b) => a + b, 0);
      const avg = students && filled ? (total / students).toFixed(1) : '—';
      return 'حقول مملوءة: ' + filled + ' · متوسط الفصل: ' + avg;
    }
    if (type === 'absence') {
      const count = Object.values(this.absenceValues()).filter(v => v).length;
      return 'إجمالي الغياب: ' + count;
    }
    return '';
  });

  fillZeros() {
    const students = this.getStudents();
    const tmpl = this.currentTmpl();
    if (!tmpl) return;
    students.forEach(st => {
      tmpl.criteria.forEach(c => this.setGrade(st.id, c.id, 0));
    });
  }

  fillMax() {
    const students = this.getStudents();
    const tmpl = this.currentTmpl();
    if (!tmpl) return;
    students.forEach(st => {
      tmpl.criteria.forEach(c => this.setGrade(st.id, c.id, c.max));
    });
  }

  clearGrades() {
    this.gradeValues.set({});
    this.absenceValues.set({});
    this.examValues.set({});
    this.finalValues.set({});
  }

  getGradeValue(sid: number, cid: string): number {
    return this.gradeValues()[sid + '_' + this.entryWeek() + '_' + cid] || 0;
  }

  onGradeInput(sid: number, cid: string, event: Event) {
    const val = parseInt((event.target as HTMLInputElement).value) || 0;
    this.setGrade(sid, cid, val);
  }

  onExamInput(sid: number, examNum: number, event: Event) {
    const val = parseInt((event.target as HTMLInputElement).value) || 0;
    this.setExam(sid, examNum, val);
  }

  onFinalInput(sid: number, field: string, event: Event) {
    const val = parseInt((event.target as HTMLInputElement).value) || 0;
    this.setFinal(sid, field, val);
  }

  getGradeDisplay(sid: number, cid: string): string {
    const v = this.gradeValues()[sid + '_' + this.entryWeek() + '_' + cid];
    return v !== undefined ? '' + v : '';
  }
}

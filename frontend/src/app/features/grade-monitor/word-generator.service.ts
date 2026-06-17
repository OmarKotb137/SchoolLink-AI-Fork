import { Injectable } from '@angular/core';
import {
  Document, Packer, Paragraph, TextRun, PageBreak,
  Table, TableRow, TableCell,
  AlignmentType, PageOrientation, BorderStyle, WidthType,
  ShadingType, VerticalAlign, TableLayoutType, UnderlineType,
} from 'docx';

export interface WordGenParams {
  schoolInfo: { directorate: string; administration: string; school: string };
  title: string;
  subject: string;
  classRoom: string;
  gradeLevel: string;
  termName: string;
  academicYearName: string;
  students: { id: number; name: string }[];
}

export interface WeeklyGradesParams extends WordGenParams {
  weeks: { name: string; date: string }[];
  criteria: { id: string; name: string; max: number }[];
  gradeData: Record<string, { total: number; values: Record<string, number> }>;
  weeklyMax: number;
  studentsPerPage?: number;
}

export interface AttendanceSheetParams extends WordGenParams {
  weeks: { name: string; date: string }[];
  days: string[];
  absenceData: Record<string, { total: number; days: Record<string, boolean> }>;
  avgMax: number;
  studentsPerPage?: number;
}

export interface SummaryGradesParams extends WordGenParams {
  gradeColumns: { id: string; label: string; max: number }[];
  monthAverages: Record<number, Record<string, number>>;
  examData: Record<number, { exam1?: number; exam2?: number }>;
  finalData: Record<number, { written_total?: number; final_exam?: number; total?: number }>;
  monthGroups: { name: string; weeks: number[] }[];
  gradeData: Record<string, { total: number }>;
  studentsPerPage?: number;
}

function t(text: string, opts?: any) {
  return new TextRun({ text, font: 'Traditional Arabic', rightToLeft: true, ...opts });
}

function tp(children: any[], opts?: any) {
  return new Paragraph({
    alignment: AlignmentType.CENTER, bidirectional: true, spacing: { before: 0, after: 0 }, ...opts,
    children,
  });
}

const PAGE_W = 11907, PAGE_H = 16840, MARGIN = 80, TABLE_W = PAGE_W - MARGIN * 2;
const NO_BORDER = { style: BorderStyle.NONE, size: 0, color: 'FFFFFF' };
const NO_BORDERS = { top: NO_BORDER, bottom: NO_BORDER, left: NO_BORDER, right: NO_BORDER, insideHorizontal: NO_BORDER, insideVertical: NO_BORDER };

@Injectable({ providedIn: 'root' })
export class WordGeneratorService {
  private cell(text: string, w: number, opts?: any) {
    const o = { bold: false, sz: 18, bg: null as string | null, vMergeRestart: false, vMerge: false, colSpan: 1, align: AlignmentType.CENTER, ...opts };
    return new TableCell({
      width: { size: w, type: WidthType.DXA }, borders: this.boldBorders(),
      margins: { top: 10, bottom: 10, left: 10, right: 10 },
      verticalAlign: VerticalAlign.CENTER,
      shading: o.bg ? { fill: o.bg, type: ShadingType.CLEAR } : undefined,
      verticalMerge: o.vMergeRestart ? 'restart' : o.vMerge ? 'continue' : undefined,
      columnSpan: o.colSpan,
      children: [new Paragraph({
        alignment: o.align, bidirectional: true, spacing: { before: 0, after: 0, line: 200 },
        children: [t(text, { bold: o.bold, size: o.sz })],
      })],
    });
  }

  private verticalCell(text: string, w: number, gray?: boolean) {
    return new TableCell({
      width: { size: w, type: WidthType.DXA }, borders: this.boldBorders(),
      textDirection: 'btLr', verticalAlign: VerticalAlign.CENTER,
      shading: { fill: gray ? 'EAEAEA' : 'FFFFFF', type: ShadingType.CLEAR },
      margins: { top: 5, bottom: 5, left: 5, right: 5 },
      children: [new Paragraph({
        alignment: AlignmentType.CENTER, spacing: { before: 0, after: 0 },
        children: [t(text, { bold: true, size: 18 })],
      })],
    });
  }

  private headerCell(text: string, w: number) {
    return new TableCell({
      width: { size: w, type: WidthType.DXA },
      borders: NO_BORDERS,
      margins: { top: 5, bottom: 5, left: 5, right: 5 },
      children: [new Paragraph({
        alignment: AlignmentType.LEFT, bidirectional: true, indent: { right: 120 }, spacing: { before: 0, after: 0 },
        children: [t(text, { bold: true, size: 24 })],
      })],
    });
  }

  private headerTable(schoolInfo: { directorate: string; administration: string; school: string }) {
    return new Table({
      width: { size: 4200, type: WidthType.DXA }, alignment: AlignmentType.RIGHT,
      borders: NO_BORDERS,
      rows: [
        new TableRow({ children: [this.headerCell(schoolInfo.directorate, 5000)] }),
        new TableRow({ children: [this.headerCell(schoolInfo.administration, 5000)] }),
        new TableRow({ children: [this.headerCell(schoolInfo.school, 5000)] }),
      ],
    });
  }

  private boldBorders() {
    const b = { style: BorderStyle.SINGLE, size: 8, color: '000000' };
    return { top: b, bottom: b, left: b, right: b };
  }

  private thinBorders() {
    const b = { style: BorderStyle.SINGLE, size: 2, color: '666666' };
    return { top: b, bottom: b, left: b, right: b };
  }

  private titlePara(recordType: string, gradeLevel: string, termName: string, academicYearName: string) {
    const text = `سجل رصد ${recordType} ${gradeLevel} ${termName} ${academicYearName}م`;
    return new Paragraph({
      alignment: AlignmentType.CENTER, bidirectional: true, spacing: { before: 20, after: 20 },
      children: [t(text, { bold: true, size: 28 })],
    });
  }

  private subTitlePara(subject: string, classRoom: string) {
    return new Paragraph({
      alignment: AlignmentType.CENTER, bidirectional: true, spacing: { before: 0, after: 40 },
      children: [
        t('مادة / ', { bold: true, size: 24 }),
        t(subject || '..................................................', { bold: true, size: 24, underline: { type: UnderlineType.SINGLE } }),
        new TextRun({ text: '          ', font: 'Traditional Arabic' }),
        t('فصل ', { bold: true, size: 24 }),
        t(classRoom, { bold: true, size: 24, underline: { type: UnderlineType.SINGLE } }),
      ],
    });
  }

  private footerPara() {
    return new Paragraph({
      alignment: AlignmentType.CENTER, bidirectional: true, spacing: { before: 200, after: 0 },
      children: [t('معلم المادة                                  المعلم الأول المشرف                         موجه المادة                          مدير المدرسة', { bold: true, size: 22 })],
    });
  }

  private pageProps() {
    return {
      size: { width: PAGE_H, height: PAGE_W, orientation: PageOrientation.LANDSCAPE },
      margin: { top: 80, right: 80, bottom: 80, left: 80 },
    };
  }

  // ═══════════════════════════════════════════════════════
  //  WEEKLY GRADES
  // ═══════════════════════════════════════════════════════
  generateWeeklyGrades(params: WeeklyGradesParams): Promise<Blob> {
    const { schoolInfo, title, subject, classRoom, gradeLevel, termName, academicYearName, students, weeks, criteria, gradeData, weeklyMax, studentsPerPage } = params;
    const spc = studentsPerPage || 42;
    const HEADER_BG = 'EAEAEA', SCORE_BG = 'FFFFFF';
    const critCount = criteria.length;
    const SUB_SCORES = [...criteria.map(c => c.max), criteria.reduce((a, c) => a + c.max, 0)];
    const subHeaders = [...criteria.map(c => c.name), 'المجموع'];
    const AVG_SCORE = weeklyMax ?? criteria.reduce((a, c) => a + c.max, 0);
    const WEEKS_PER_PAGE = 4;

    const sections: any[] = [];
    for (let wkStart = 0; wkStart < weeks.length; wkStart += WEEKS_PER_PAGE) {
      const chunkWeeks = weeks.slice(wkStart, wkStart + WEEKS_PER_PAGE);

      const C_NUM = 450, C_NAME = 2600, C_SUB = 650, C_AVG = 700;
      const rawTotal = C_NUM + C_NAME + C_SUB * (critCount + 1) * chunkWeeks.length + C_AVG;
      const scale = TABLE_W / rawTotal;
      const W_NUM = Math.round(C_NUM * scale);
      const W_NAME = Math.round(C_NAME * scale);
      const W_SUB = Math.round(C_SUB * scale);
      const W_AVG = Math.round(C_AVG * scale);
      const allColWidths = [W_AVG, ...chunkWeeks.slice().reverse().flatMap(() => Array(critCount + 1).fill(W_SUB)), W_NAME, W_NUM];

      const row1 = new TableRow({
        tableHeader: true, height: { value: 500, rule: 'exact' },
        children: [
          this.cell('المتوسط', W_AVG, { bold: true, sz: 20, bg: HEADER_BG, vMergeRestart: true }),
          ...chunkWeeks.slice().reverse().map(w => this.cell(w.name + '\n' + w.date, W_SUB * (critCount + 1), { bold: true, sz: 20, bg: HEADER_BG, colSpan: critCount + 1 })),
          this.cell('الاســــم', W_NAME, { bold: true, sz: 22, bg: HEADER_BG, vMergeRestart: true }),
          this.cell('م', W_NUM, { bold: true, sz: 22, bg: HEADER_BG, vMergeRestart: true }),
        ],
      });

      const row2 = new TableRow({
        tableHeader: true, height: { value: 1200, rule: 'exact' },
        children: [
          this.cell('', W_AVG, { bg: HEADER_BG, vMerge: true }),
          ...chunkWeeks.slice().reverse().flatMap(() => Array.from({ length: critCount + 1 }, (_, i) => critCount - i).map(i => this.verticalCell(subHeaders[i], W_SUB))),
          this.cell('', W_NAME, { bg: HEADER_BG, vMerge: true }),
          this.cell('', W_NUM, { bg: HEADER_BG, vMerge: true }),
        ],
      });

      const row3 = new TableRow({
        height: { value: 200, rule: 'exact' },
        children: [
          this.cell(String(AVG_SCORE), W_AVG, { bold: true, sz: 18, bg: SCORE_BG }),
          ...chunkWeeks.slice().reverse().flatMap(() => Array.from({ length: critCount + 1 }, (_, i) => critCount - i).map(i => this.cell(String(SUB_SCORES[i]), W_SUB, { bold: true, sz: 18, bg: SCORE_BG }))),
          this.cell('', W_NAME, { bg: HEADER_BG, vMerge: true }),
          this.cell('', W_NUM, { bg: HEADER_BG, vMerge: true }),
        ],
      });

      for (let stStart = 1; stStart <= students.length; stStart += spc) {
        const stCount = Math.min(spc, students.length - stStart + 1);
        const pageStudentRows = [];
        for (let i = stStart; i < stStart + stCount; i++) {
          const st = students[i - 1];
          let avgSum = 0, avgCnt = 0;
          for (let wi = 0; wi < weeks.length; wi++) {
            const g = gradeData[`${wi + 1}_${st.id}`];
            if (g?.total != null) { avgSum += g.total; avgCnt++; }
          }
          pageStudentRows.push(new TableRow({
            height: { value: 250, rule: 'atLeast' },
            children: [
              this.cell(avgCnt ? String(Math.round(avgSum / avgCnt)) : '', W_AVG),
              ...chunkWeeks.slice().reverse().flatMap((_w, revIdx) => {
                const absWkNum = wkStart + chunkWeeks.length - revIdx;
                const g = gradeData[`${absWkNum}_${st.id}`];
                const vals = g?.values ?? {};
                const cells = [this.cell(g ? String(g.total ?? '') : '', W_SUB)];
                for (let ci = critCount - 1; ci >= 0; ci--) cells.push(this.cell(criteria[ci] ? String(vals[criteria[ci].id] ?? '') : '', W_SUB));
                return cells;
              }),
              this.cell(st.name, W_NAME, { bold: true, sz: 18 }),
              this.cell(String(i), W_NUM, { sz: 18, bold: true }),
            ],
          }));
        }

        sections.push({
          properties: { page: this.pageProps() },
          children: [
            this.headerTable(schoolInfo), new Paragraph({ text: '' }),
            this.titlePara('درجات', gradeLevel, termName, academicYearName),
            this.subTitlePara(subject, classRoom),
            new Table({ width: { size: TABLE_W, type: WidthType.DXA }, layout: TableLayoutType.FIXED, columnWidths: allColWidths, rows: [row1, row2, row3, ...pageStudentRows] }),
            this.footerPara(),
          ],
        });
      }
    }

    return Packer.toBlob(new Document({ sections }));
  }

  // ═══════════════════════════════════════════════════════
  //  ATTENDANCE SHEET
  // ═══════════════════════════════════════════════════════
  generateAttendanceSheet(params: AttendanceSheetParams): Promise<Blob> {
    const { schoolInfo, title, subject, classRoom, gradeLevel, termName, academicYearName, students, weeks, absenceData, days, avgMax, studentsPerPage } = params;
    const spc = studentsPerPage || 21;
    const HEADER_BG = 'EAEAEA';
    const WEEKS_PER_PAGE = 4;
    const DAYS = days.length ? days : ['الخميس', 'الأربعاء', 'الثلاثاء', 'الاثنين', 'الأحد'];

    const sections: any[] = [];
    for (let wkStart = 0; wkStart < weeks.length; wkStart += WEEKS_PER_PAGE) {
      const chunkWeeks = weeks.slice(wkStart, wkStart + WEEKS_PER_PAGE);

      const C_NUM = 450, C_NAME = 2600, C_SUB = 520, C_AVG = 500;
      const rawTotal = C_NUM + C_NAME + C_AVG + C_SUB * DAYS.length * chunkWeeks.length;
      const scale = TABLE_W / rawTotal;
      const W_NUM = Math.round(C_NUM * scale), W_NAME = Math.round(C_NAME * scale);
      const W_SUB = Math.round(C_SUB * scale), W_AVG = Math.round(C_AVG * scale);
      const allColWidths = [W_AVG, ...chunkWeeks.slice().reverse().flatMap(() => DAYS.map(() => W_SUB)), W_NAME, W_NUM];
      const BORDERS = this.thinBorders();

      const mkCell = (text: string, w: number, opts?: any) => {
        const o = { bold: false, sz: 18, bg: null as string | null, vMergeRestart: false, vMerge: false, colSpan: 1, align: AlignmentType.CENTER, ...opts };
        return new TableCell({
          width: { size: w, type: WidthType.DXA }, borders: BORDERS,
          margins: { top: 10, bottom: 10, left: 10, right: 10 },
          verticalAlign: VerticalAlign.CENTER,
          shading: o.bg ? { fill: o.bg, type: ShadingType.CLEAR } : undefined,
          verticalMerge: o.vMergeRestart ? 'restart' : o.vMerge ? 'continue' : undefined,
          columnSpan: o.colSpan,
          children: [new Paragraph({
            alignment: o.align, bidirectional: true, spacing: { before: 0, after: 0 },
            children: [t(text, { bold: o.bold, size: o.sz })],
          })],
        });
      };

      const vCell = (text: string, w: number, gray: boolean) => new TableCell({
        width: { size: w, type: WidthType.DXA }, borders: BORDERS,
        textDirection: 'btLr', verticalAlign: VerticalAlign.CENTER,
    shading: { fill: gray === false ? 'FFFFFF' : 'EAEAEA', type: ShadingType.CLEAR },
        children: [new Paragraph({
          alignment: AlignmentType.CENTER, indent: { right: 80 }, spacing: { before: 0, after: 0 },
          children: [t(text, { bold: true, size: 20 })],
        })],
      });

      const row1 = new TableRow({
        tableHeader: true, height: { value: 400, rule: 'exact' },
        children: [
          mkCell('المتوسط', W_AVG, { bold: true, sz: 18, bg: HEADER_BG, vMergeRestart: true }),
          ...chunkWeeks.slice().reverse().map(w => mkCell(w.name + ' ' + w.date, W_SUB * DAYS.length, { bold: true, sz: 18, bg: HEADER_BG, colSpan: DAYS.length })),
          mkCell('الاســــم', W_NAME, { bold: true, sz: 22, bg: HEADER_BG, vMergeRestart: true }),
          mkCell('م', W_NUM, { bold: true, sz: 22, bg: HEADER_BG, vMergeRestart: true }),
        ],
      });

      const row2 = new TableRow({
        tableHeader: true, height: { value: 1400, rule: 'exact' },
        children: [
          mkCell(String(avgMax ?? 5), W_AVG, { bold: true, sz: 18, bg: HEADER_BG }),
          ...chunkWeeks.slice().reverse().flatMap(() => DAYS.map((d, i) => vCell(d, W_SUB, i % 2 === 0))),
          mkCell('', W_NAME, { bg: HEADER_BG, vMerge: true }),
          mkCell('', W_NUM, { bg: HEADER_BG, vMerge: true }),
        ],
      });

      for (let stStart = 1; stStart <= students.length; stStart += spc) {
        const stCount = Math.min(spc, students.length - stStart + 1);
        const studentRows = [];
        for (let i = stStart; i < stStart + stCount; i++) {
          const st = students[i - 1];
          let absSum = 0, absCnt = 0;
          for (let wi = 0; wi < weeks.length; wi++) {
            const a = absenceData[`${wi + 1}_${st.id}`];
            if (a?.total != null) { 
              // Score per week = maxScore (avgMax) minus the number of absences that week
              const weekScore = Math.max(0, avgMax - a.total);
              absSum += weekScore; 
              absCnt++; 
            }
          }
          studentRows.push(new TableRow({
            height: { value: 300, rule: 'exact' },
            children: [
              mkCell(absCnt ? String(Math.round(absSum / absCnt)) : '', W_AVG),
              ...chunkWeeks.slice().reverse().flatMap((_w, revIdx) => {
                const absWkNum = wkStart + chunkWeeks.length - revIdx;
                const a = absenceData[`${absWkNum}_${st.id}`];
                const d = a?.days ?? {};
                return DAYS.map(day => mkCell(d[day] ? 'غ' : '', W_SUB));
              }),
              mkCell(st.name, W_NAME, { bold: true, sz: 18 }),
              mkCell(String(i), W_NUM, { sz: 18 }),
            ],
          }));
        }

        sections.push({
          properties: { page: this.pageProps() },
          children: [
            this.headerTable(schoolInfo), new Paragraph({ text: '' }),
            this.titlePara('غياب', gradeLevel, termName, academicYearName),
            this.subTitlePara(subject, classRoom),
            new Table({ width: { size: TABLE_W, type: WidthType.DXA }, layout: TableLayoutType.FIXED, columnWidths: allColWidths, rows: [row1, row2, ...studentRows] }),
            this.footerPara(),
          ],
        });
      }
    }

    return Packer.toBlob(new Document({ sections }));
  }

  // ═══════════════════════════════════════════════════════
  //  SUMMARY GRADES
  // ═══════════════════════════════════════════════════════
  generateSummaryGrades(params: SummaryGradesParams): Promise<Blob> {
    const { schoolInfo, title, subject, classRoom, gradeLevel, termName, academicYearName, students, gradeColumns, monthAverages, examData, finalData, monthGroups, gradeData, studentsPerPage } = params;
    const spc = studentsPerPage || 21;
    const HEADER_BG = 'EAEAEA', SCORE_BG = 'FFFFFF';

    const GRADE_COLUMNS = gradeColumns.length ? gradeColumns : [
      { id: 'feb_avg', label: 'متوسط\nشهر\nفبراير', max: 40 },
      { id: 'mar_avg', label: 'متوسط\nشهر\nمارس', max: 40 },
      { id: 'apr_avg', label: 'متوسط\nشهر\nأبريل', max: 40 },
      { id: 'all_avg', label: 'متوسط\nالثلاثة\nشهور', max: 40 },
      { id: 'exam1', label: 'الاختبار\nالشهري\nالأول', max: 15 },
      { id: 'exam2', label: 'الاختبار\nالشهري\nالثاني', max: 15 },
      { id: 'written_total', label: 'مجموع\nالأعمال\nالتحريرية', max: 70 },
      { id: 'end_year', label: 'درجة\nآخر\nالعام', max: 30 },
      { id: 'final_total', label: 'المجموع\nالنهائي', max: 100 },
    ];

    const RAW_NUM = 400, RAW_NAME = 2800, RAW_COL = 750;
    const rawTotal2 = RAW_NUM + RAW_NAME + RAW_COL * GRADE_COLUMNS.length;
    const scale = TABLE_W / rawTotal2;
    const W_NUM = Math.round(RAW_NUM * scale), W_NAME = Math.round(RAW_NAME * scale), W_COL = Math.round(RAW_COL * scale);
    const allColWidths = [...GRADE_COLUMNS.slice().reverse().map(() => W_COL), W_NAME, W_NUM];
    const BORDERS = this.thinBorders();

    const makeCell = (text: string, w: number, opts?: any) => {
      const o = { bold: false, sz: 18, bg: null as string | null, vMergeRestart: false, vMerge: false, ...opts };
      return new TableCell({
        width: { size: w, type: WidthType.DXA }, borders: BORDERS,
        margins: { top: 40, bottom: 40, left: 60, right: 60 },
        verticalAlign: VerticalAlign.CENTER,
        shading: o.bg ? { fill: o.bg, type: ShadingType.CLEAR } : undefined,
        verticalMerge: o.vMergeRestart ? 'restart' : o.vMerge ? 'continue' : undefined,
        children: [new Paragraph({
          alignment: AlignmentType.CENTER, bidirectional: true, spacing: { before: 0, after: 0 },
          children: [t(text, { bold: o.bold, size: o.sz })],
        })],
      });
    };

    const row1 = new TableRow({
      tableHeader: true, height: { value: 600, rule: 'exact' },
      children: [
        ...GRADE_COLUMNS.slice().reverse().map((col, idx) => makeCell(col.label, W_COL, { bold: true, sz: idx === 0 ? 20 : 16, bg: HEADER_BG, vMergeRestart: idx === 0 })),
        makeCell('الاسم', W_NAME, { bold: true, sz: 20, bg: HEADER_BG, vMergeRestart: true }),
        makeCell('م', W_NUM, { bold: true, sz: 20, bg: HEADER_BG, vMergeRestart: true }),
      ],
    });

    const row2 = new TableRow({
      height: { value: 300, rule: 'exact' },
      children: [
        makeCell('', W_COL, { bg: HEADER_BG, vMerge: true }),
        ...GRADE_COLUMNS.slice().reverse().slice(1).map(col => makeCell(String(col.max), W_COL, { bold: true, sz: 18, bg: SCORE_BG })),
        makeCell('', W_NAME, { bg: HEADER_BG, vMerge: true }),
        makeCell('', W_NUM, { bg: HEADER_BG, vMerge: true }),
      ],
    });

    const MONTH_MAP: Record<string, string> = { sep: 'سبتمبر', oct: 'أكتوبر', nov: 'نوفمبر', dec: 'ديسمبر', jan: 'يناير', feb: 'فبراير', mar: 'مارس', apr: 'أبريل', may: 'مايو', jun: 'يونيو', jul: 'يوليو', aug: 'أغسطس' };

    const getSummaryValue = (stId: number, col: { id: string; max: number }): string => {
      const ma = monthAverages?.[stId] ?? {};
      const ed = examData?.[stId] ?? {};
      const fd = finalData?.[stId] ?? {};
      const mg = monthGroups || [];
      const monthMatch = col.id.match(/^(.+)_avg$/);
      if (monthMatch) {
        const monName = MONTH_MAP[monthMatch[1]];
        if (monName) {
          const idx = mg.findIndex(m => m.name === monName);
          return String(ma[monName] ?? (idx >= 0 ? (() => { const mg2 = mg[idx]; if (!mg2?.weeks) return null; let s = 0, c = 0; for (const wn of mg2.weeks) { const g = gradeData[`${wn}_${stId}`]; if (g?.total != null) { s += g.total; c++; } } return c ? Math.round(s / c) : null; })() : '') ?? '');
        }
      }
      switch (col.id) {
        case 'all_avg': case 'three_month_avg': { const vals: number[] = []; for (let i = 0; i < mg.length; i++) { const mg2 = mg[i]; const v = ma[mg2?.name] ?? (() => { if (!mg2?.weeks) return null; let s = 0, c = 0; for (const wn of mg2.weeks) { const g = gradeData[`${wn}_${stId}`]; if (g?.total != null) { s += g.total; c++; } } return c ? Math.round(s / c) : null; })(); if (v != null) vals.push(v); } return vals.length ? String(Math.round(vals.reduce((a, b) => a + b, 0) / vals.length)) : ''; }
        case 'exam1': return String(ed['exam1'] ?? '');
        case 'exam2': return String(ed['exam2'] ?? '');
        case 'written_total': return String(fd['written_total'] ?? '');
        case 'end_year': return String(fd['final_exam'] ?? '');
        case 'final_total': return String(fd['total'] ?? '');
        default: return '';
      }
    };

    const pages: any[] = [];
    for (let start = 1; start <= students.length; start += spc) {
      const end = Math.min(start + spc - 1, students.length);
      const studentRows = [];
      for (let i = start; i <= end; i++) {
        const st = students[i - 1];
        studentRows.push(new TableRow({
          height: { value: 250, rule: 'atLeast' },
          children: [
            ...GRADE_COLUMNS.slice().reverse().map(col => makeCell(getSummaryValue(st.id, col), W_COL)),
            makeCell(st.name, W_NAME, { bold: true, sz: 18 }),
            makeCell(String(i), W_NUM, { sz: 18 }),
          ],
        }));
      }

      const table = new Table({
        width: { size: TABLE_W, type: WidthType.DXA }, columnWidths: allColWidths,
        layout: TableLayoutType.FIXED, rows: [row1, row2, ...studentRows],
      });

      pages.push(
        this.headerTable(schoolInfo), new Paragraph({ text: '' }),
        this.titlePara('الدرجات', gradeLevel, termName, academicYearName), this.subTitlePara(subject, classRoom),
        table, this.footerPara(),
      );
      if (end < students.length) pages.push(new Paragraph({ children: [new PageBreak()] }));
    }

    return Packer.toBlob(new Document({
      sections: [{ properties: { page: this.pageProps() }, children: pages }],
    }));
  }
}

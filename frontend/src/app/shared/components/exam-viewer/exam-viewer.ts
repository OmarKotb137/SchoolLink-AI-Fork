import { Component, Input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

export interface ExamViewerGroup {
  id: number;
  displayType: number;
  contentTitle?: string;
  contentText?: string;
  imageUrl?: string;
  displayOrder: number;
  questions: ExamViewerQuestion[];
}

export interface ExamViewerQuestion {
  id: number;
  groupId?: number;
  displayType: number;
  contentText?: string;
  questionText: string;
  questionType: number;
  imageUrl?: string;
  points: number;
  displayOrder: number;
  options: ExamViewerOption[];
}

export interface ExamViewerOption {
  id: number;
  optionText: string;
  displayOrder: number;
}

@Component({
  selector: 'app-exam-viewer',
  standalone: true,
  template: `
    <div class="exam-page" dir="rtl">
      <div class="exam-header">
        <h1>{{ title }}</h1>
        <div class="exam-meta">
          <span>المادة: {{ subject }}</span>
          @if (duration) { <span>الزمن: {{ duration }} دقيقة</span> }
          <span>الدرجة الكلية: {{ totalScore }}</span>
        </div>
      </div>

      @if (htmlContent) {
        <div [innerHTML]="htmlContent"></div>
      } @else {
        <div class="exam-body">
          @for (block of blocks; track block.id; let i = $index) {
            <div class="exam-block">
              <!-- Group content -->
              @if (block.displayType === 1 && block.contentText) {
                <table class="exam-content">
                  <tr><td>
                    @if (block.contentTitle) {
                      <div class="passage-title">{{ block.contentTitle }}</div>
                    }
                    <div class="passage-text">{{ block.contentText }}</div>
                  </td></tr>
                </table>
              }
              @if ((block.displayType === 2 || block.displayType === 3) && block.imageUrl) {
                <figure class="exam-figure">
                  <img [src]="block.imageUrl" [alt]="block.contentText || ''" />
                  @if (block.contentText) {
                    <figcaption>{{ block.contentText }}</figcaption>
                  }
                </figure>
              }
              @if (block.displayType === 4 && block.contentText) {
                <div class="exam-figure" [innerHTML]="sanitizeSvg(block.contentText)"></div>
              }

              <!-- Questions -->
              @for (q of block.questions; track q.id; let j = $index) {
                <div class="q-item">
                  <span class="q-points">({{ q.points }} درجة)</span>
                  <p class="q-text">{{ currentNumber() + j }}. {{ q.questionText }}</p>
                  @if (q.questionType === 1 || q.questionType === 4) {
                    <ol class="q-options" type="A">
                      @for (opt of q.options; track opt.id) {
                        <li>{{ opt.optionText }}</li>
                      }
                    </ol>
                  }
                  @if (q.questionType === 3) {
                    <div class="answer-line tall"></div>
                    <div class="answer-line tall"></div>
                  }
                  @if (q.questionType === 2) {
                    <div class="answer-line"></div>
                  }
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .exam-page {
      font-family: 'Cairo', 'Times New Roman', serif;
      color: #000;
      background: #fff;
      font-size: 14px;
      line-height: 1.7;
      padding: 18mm 15mm;
      max-width: 210mm;
      margin: 0 auto;
    }
    .exam-header {
      text-align: center;
      border-bottom: 2px solid #000;
      padding-bottom: 10px;
      margin-bottom: 18px;
    }
    .exam-header h1 {
      margin: 0 0 4px;
      font-size: 20px;
      font-weight: 700;
    }
    .exam-meta {
      display: flex;
      justify-content: space-between;
      font-size: 13px;
    }
    .exam-body { padding: 0 4px; }
    .exam-block { margin-bottom: 22px; }
    .passage-title {
      margin: 0 0 8px;
      font-size: 15px;
      font-weight: 700;
    }
    .passage-text { text-align: justify; }
    .exam-figure { text-align: center; margin: 10px 0; }
    .exam-figure img {
      max-width: 100%;
      max-height: 380px;
      filter: grayscale(100%);
      display: inline-block;
      border: 1px solid #000;
    }
    .exam-figure figcaption { font-size: 12px; margin-top: 5px; }
    .q-item { margin: 10px 0; padding: 4px 0; }
    .q-text { font-weight: 600; margin: 0 0 4px; }
    .q-options { list-style: upper-alpha; padding-inline-start: 24px; margin: 4px 0; }
    .q-options li { margin: 2px 0; }
    .q-points { font-size: 12px; float: left; }
    .answer-line { border-bottom: 1px dotted #000; min-height: 22px; margin: 6px 0; width: 80%; }
    .answer-line.tall { min-height: 60px; }
    table.exam-content { width: 100%; border-collapse: collapse; margin-bottom: 12px; }
    table.exam-content td, table.exam-content th { border: 1px solid #000; padding: 8px; }
    table.exam-content th { background: #f0f0f0; font-weight: 700; }
    @media print {
      .exam-page { padding: 0; }
    }
  `]
})
export class ExamViewerComponent {
  @Input() title = '';
  @Input() subject = '';
  @Input() duration: number | null = null;
  @Input() totalScore = 0;
  @Input() blocks: ExamViewerBlock[] = [];
  @Input() htmlContent: SafeHtml | null = null;

  private questionCounter = 0;

  constructor(private sanitizer: DomSanitizer) {}

  currentNumber(): number {
    const base = this.questionCounter;
    this.questionCounter += 1;
    return base;
  }

  sanitizeSvg(svg: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(svg);
  }

  resetCounter() {
    this.questionCounter = 0;
  }
}

export interface ExamViewerBlock {
  id: number;
  displayType: number;
  contentTitle?: string;
  contentText?: string;
  imageUrl?: string;
  questions: ExamViewerQuestion[];
}

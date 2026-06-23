import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { StudentExamResultAnswer, StudentExamAttemptResult } from '../../core/models/student-exam.models';
import { StudentExamsService } from '../../core/services/student-exams.service';

@Component({
  selector: 'app-exam-result',
  standalone: true,
  imports: [CommonModule, Sidebar],
  templateUrl: './exam-result.html',
  styleUrl: './exam-result.css'
})
export class ExamResult implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private examsService = inject(StudentExamsService);

  result = signal<StudentExamAttemptResult | null>(null);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  sidebarOpen = signal(false);

  private attemptId = Number(this.route.snapshot.paramMap.get('attemptId'));

  ngOnInit() {
    this.loadResult();
  }

  loadResult() {
    this.isLoading.set(true);
    this.examsService.getAttemptResult(this.attemptId).subscribe({
      next: response => {
        this.result.set(response.data);
        this.isLoading.set(false);
      },
      error: err => {
        this.errorMessage.set(this.extractErrorMessage(err, 'تعذر تحميل حالة النتيجة'));
        this.isLoading.set(false);
      }
    });
  }

  backToExams() {
    this.router.navigate(['/my-exams']);
  }

  getScorePercent(result: StudentExamAttemptResult): number {
    if (!result.totalScore || result.score == null) return 0;
    return Math.round((result.score / result.totalScore) * 100);
  }

  /**
   * مستوى أداء الطالب حسب النسبة المئوية.
   *
   * بيحدّد لون دايرة الدرجة (وعندنا result-char بيفضل binary pass/fail عند 50%
   * لأنه عنصر تحفيزي عاطفي، فالمستويات الأربعة بتظهر بس في الدايرة).
   *
   * العتبات:
   *   >= 85  ممتاز (أخضر)   |  >= 70 جيد (أزرق)
   *   >= 50  مقبول (كهرماني) |  <  50 راسب (أحمر)
   */
  scoreTier(result: StudentExamAttemptResult): 'excellent' | 'good' | 'pass' | 'fail' {
    const percent = this.getScorePercent(result);
    if (percent >= 85) return 'excellent';
    if (percent >= 70) return 'good';
    if (percent >= 50) return 'pass';
    return 'fail';
  }

  /**
   * هل نعرض الإجابة النموذجية لهذه الإجابة؟
   *
   * القاعدة: نعرضها كل ما الطالب ما يأخدش الدرجة الكاملة للسؤال،
   * سواء كانت إجابة غلطة بالكامل أو درجة جزئية (مقالي/أكمل فراغ).
   * أما الدرجة الكاملة فإجابته هي النموذجية ولا داعي لتكرارها.
   *
   * ملاحظة: القاعدة السابقة كانت تعتمد على isCorrect === false فقط،
   * فكانت تخفي الإجابة النموذجية عند الدرجة الجزئية (لأن isCorrect=true
   * لو الدرجة > 0)، مما يفقد الطالب فرصة المقارنة عند الحاجة لها أكثر.
   */
  shouldShowModelAnswer(answer: StudentExamResultAnswer): boolean {
    return !!answer.correctAnswerText && answer.pointsEarned < answer.questionPoints;
  }

  /**
   * حالة الإجابة بناءً على الدرجة المُكتسبة مقابل درجة السؤال الكاملة.
   *
   * ده المصدر الموحّد للحالة اللي بيعتمد عليه الـ template في التلوين،
   * عشان نقارن الدرجة فعلياً بدل الاعتماد على isCorrect — اللي بيبقى true
   * لأي درجة > 0، فالدرجة الجزئية كانت بتظهر كأنها "صحيحة" بالغلط.
   */
  answerState(answer: StudentExamResultAnswer): 'correct' | 'partial' | 'wrong' {
    if (answer.pointsEarned >= answer.questionPoints) return 'correct';
    if (answer.pointsEarned > 0) return 'partial';
    return 'wrong';
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    const error = err as { error?: { message?: string }; message?: string };
    return error.error?.message || error.message || fallback;
  }
}

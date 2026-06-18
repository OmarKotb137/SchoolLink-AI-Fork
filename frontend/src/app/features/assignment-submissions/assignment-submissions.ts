import { Component, signal, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { AssignmentManagerService, AssignmentSubmissionListItem, AssignmentSubmissionDetail, AssignmentDetail } from '../assignment-management/assignment-manager.service';

@Component({
  selector: 'app-assignment-submissions',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, Sidebar],
  templateUrl: './assignment-submissions.html',
  styleUrl: './assignment-submissions.css'
})
export class AssignmentSubmissions implements OnInit {
  private api = inject(AssignmentManagerService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  sidebarOpen = signal(false);
  assignmentId = signal<number>(0);
  assignment = signal<AssignmentDetail | null>(null);
  submissions = signal<AssignmentSubmissionListItem[]>([]);
  
  viewingSubmission = signal<AssignmentSubmissionDetail | null>(null);
  manualGrades = signal<Record<number, number>>({});
  
  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.assignmentId.set(+id);
        this.loadData();
      }
    });
  }

  loadData() {
    this.api.getById(this.assignmentId()).subscribe({
      next: (data) => this.assignment.set(data),
      error: () => this.router.navigate(['/assignment-management'])
    });

    this.api.getSubmissions(this.assignmentId()).subscribe(r => {
      if (r.isSuccess) {
        this.submissions.set(r.data);
      }
    });
  }

  viewSubmission(sub: AssignmentSubmissionListItem) {
    this.api.getSubmissionDetail(this.assignmentId(), sub.submissionId).subscribe(r => {
      if (r.isSuccess) {
        this.viewingSubmission.set(r.data);
        const grades: Record<number, number> = {};
        for (const ans of r.data.answers) {
          if (ans.type === 'essay') {
            grades[ans.questionId] = ans.pointsEarned || 0;
          }
        }
        this.manualGrades.set(grades);
      }
    });
  }

  closeModal() {
    this.viewingSubmission.set(null);
  }

  updateGrade(questionId: number, value: number) {
    this.manualGrades.update(g => ({ ...g, [questionId]: value }));
  }

  saveGrade() {
    const sub = this.viewingSubmission();
    if (!sub) return;

    this.api.gradeSubmission(this.assignmentId(), sub.submissionId, { manualGrades: this.manualGrades() }).subscribe(r => {
      if (r.isSuccess) {
        this.closeModal();
        this.loadData();
      } else {
        alert(r.message || 'حدث خطأ أثناء التصحيح');
      }
    });
  }
}

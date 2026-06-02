using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.AssignmentSubmission
{
    public class GetAssignmentSubmissionDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AssignmentTitle { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }
        public bool IsGraded { get; set; }
        public string? AIFeedback { get; set; }
        public List<GetAssignmentAnswerDto> Answers { get; set; } = new();
    }

    public class GetAssignmentAnswerDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? AnswerText { get; set; }
        public bool? IsCorrect { get; set; }
        public decimal PointsEarned { get; set; }
        public string? AIFeedback { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.ExamAttempt
{
    public class GetExamAttemptDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public decimal? Score { get; set; }
        public decimal TotalScore { get; set; }
        public bool IsGraded { get; set; }
        public List<GetExamAnswerDto> Answers { get; set; } = new();
    }

    public class GetExamAnswerDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;  // mcq | true-false | essay
        public decimal QuestionPoints { get; set; }               // الحد الأقصى لدرجة السؤال
        public string? AnswerText { get; set; }
        public bool? IsCorrect { get; set; }
        public decimal PointsEarned { get; set; }
        public string? Feedback { get; set; }
    }
}
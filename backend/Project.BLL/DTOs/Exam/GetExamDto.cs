using Project.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.Exam
{
    public class GetExamDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal TotalScore { get; set; }
        public bool IsAIGenerated { get; set; }
        public bool IsPublished { get; set; }
        public EvaluationCategory Category { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int QuestionsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<GetExamQuestionDto> Questions { get; set; } = new();
    }

    public class GetExamQuestionDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Points { get; set; }
        public int DisplayOrder { get; set; }
        public List<GetExamQuestionOptionDto> Options { get; set; } = new();
    }

    public class GetExamQuestionOptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
using Project.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.Assignment
{
    public class GetAssignmentDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal MaxScore { get; set; }
        public bool IsAutoGraded { get; set; }
        public bool IsAIGenerated { get; set; }
        public EvaluationCategory Category { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int QuestionsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<GetAssignmentQuestionDto> Questions { get; set; } = new();
    }

    public class GetAssignmentQuestionDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Points { get; set; }
        public int DisplayOrder { get; set; }
        public List<GetAssignmentQuestionOptionDto> Options { get; set; } = new();
    }

    public class GetAssignmentQuestionOptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
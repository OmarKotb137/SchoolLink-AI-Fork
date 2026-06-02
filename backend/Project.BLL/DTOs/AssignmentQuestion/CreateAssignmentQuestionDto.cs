using Project.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.AssignmentQuestion
{
    public class CreateAssignmentQuestionDto
    {
        public int AssignmentId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public string? ImageUrl { get; set; }
        public string? CorrectAnswer { get; set; }
        public int DisplayOrder { get; set; }
        public decimal Points { get; set; }
        public List<CreateAssignmentQuestionOptionDto> Options { get; set; } = new();
    }

    public class CreateAssignmentQuestionOptionDto
    {
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }
    }
}
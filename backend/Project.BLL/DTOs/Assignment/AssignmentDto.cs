using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.Assignment
{
    public class AssignmentDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public decimal MaxScore { get; set; }
        public bool IsAutoGraded { get; set; }
        public bool IsAIGenerated { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int QuestionsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
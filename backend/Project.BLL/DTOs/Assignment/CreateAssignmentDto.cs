using Project.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.Assignment
{
    public class CreateAssignmentDto
    {
        public int ClassSubjectTeacherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal MaxScore { get; set; }
        public bool IsAutoGraded { get; set; } = false;
        public bool IsAIGenerated { get; set; } = false;
        public EvaluationCategory Category { get; set; }
    }
}
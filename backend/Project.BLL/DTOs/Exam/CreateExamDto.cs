using Project.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.Exam
{
    public class CreateExamDto
    {
        public int? ClassSubjectTeacherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal TotalScore { get; set; }
        public bool IsAIGenerated { get; set; } = false;
        public EvaluationCategory Category { get; set; }
    }
}
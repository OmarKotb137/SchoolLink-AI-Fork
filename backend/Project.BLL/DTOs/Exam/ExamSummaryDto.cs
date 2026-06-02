using Project.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.Exam
{
    public class ExamSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal TotalScore { get; set; }
        public bool IsPublished { get; set; }
        public bool IsAIGenerated { get; set; }
        public EvaluationCategory Category { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int QuestionsCount { get; set; }
    }
}
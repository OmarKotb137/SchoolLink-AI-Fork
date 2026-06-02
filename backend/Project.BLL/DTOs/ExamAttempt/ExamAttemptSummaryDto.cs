using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.ExamAttempt
{
    public class ExamAttemptSummaryDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public decimal? Score { get; set; }
        public decimal TotalScore { get; set; }
        public bool IsGraded { get; set; }
        public int TimeTakenMinutes { get; set; }
    }
}
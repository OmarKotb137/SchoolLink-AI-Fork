using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.AssignmentSubmission
{
    public class AssignmentSubmissionSummaryDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }
        public bool IsGraded { get; set; }
    }
}
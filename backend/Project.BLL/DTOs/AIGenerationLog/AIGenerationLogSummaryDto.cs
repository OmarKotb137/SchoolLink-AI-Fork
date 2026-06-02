using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.AIGenerationLog
{
    public class AIGenerationLogSummaryDto
    {
        public string OperationType { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int? TotalTokensUsed { get; set; }
        public double AverageLatencyMs { get; set; }
    }
}
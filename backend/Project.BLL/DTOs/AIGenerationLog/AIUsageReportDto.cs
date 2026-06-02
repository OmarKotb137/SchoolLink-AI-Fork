using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.AIGenerationLog
{
    public class AIUsageReportDto
    {
        public int TotalCalls { get; set; }
        public double SuccessRate { get; set; }
        public int? TotalTokensUsed { get; set; }
        public double AverageLatencyMs { get; set; }
        public List<AIGenerationLogSummaryDto> Breakdown { get; set; } = new();
    }
}
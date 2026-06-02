using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.AIGenerationLog
{
    public class CreateAIGenerationLogDto
    {
        public int UserId { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string? InputSummary { get; set; }
        public bool IsSuccess { get; set; }
        public int? TokensUsed { get; set; }
        public int? LatencyMs { get; set; }
    }
}
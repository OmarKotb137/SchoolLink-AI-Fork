using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.AIGenerationLog
{
    public class GetUsageReportFilter
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? OperationType { get; set; }
        public int? UserId { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.ExamAttempt
{
    public class CreateExamAttemptDto
    {
        public int EnrollmentId { get; set; }
        public int ExamId { get; set; }
    }
}
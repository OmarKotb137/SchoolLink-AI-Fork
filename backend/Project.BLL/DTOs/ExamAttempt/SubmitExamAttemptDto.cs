using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.ExamAttempt
{
    public class SubmitExamAttemptDto
    {
        public int AttemptId { get; set; }
        public List<SubmitExamAnswerDto> Answers { get; set; } = new();
    }

    public class SubmitExamAnswerDto
    {
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.BLL.DTOs.AssignmentSubmission
{
    public class CreateAssignmentSubmissionDto
    {
        public int EnrollmentId { get; set; }
        public int AssignmentId { get; set; }
        public List<CreateAssignmentAnswerDto> Answers { get; set; } = new();
    }

    public class CreateAssignmentAnswerDto
    {
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; }
    }
}
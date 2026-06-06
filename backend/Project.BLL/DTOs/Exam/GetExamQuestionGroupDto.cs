using Project.Domain.Enums;

namespace Project.BLL.DTOs.Exam
{
    public class GetExamQuestionGroupDto
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public TemplateContentType DisplayType { get; set; }
        public string? ContentTitle { get; set; }
        public string? ContentText { get; set; }
        public string? ImagePrompt { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public List<GetExamQuestionDto> Questions { get; set; } = new();
    }
}

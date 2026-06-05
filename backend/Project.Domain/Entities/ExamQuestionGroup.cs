using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class ExamQuestionGroup : BaseEntity
    {
        public int ExamId { get; set; }
        public TemplateContentType DisplayType { get; set; } = TemplateContentType.Passage;
        public string? ContentTitle { get; set; }
        public string? ContentText { get; set; }
        public string? ImagePrompt { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }

        public Exam Exam { get; set; } = null!;
        public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
    }
}

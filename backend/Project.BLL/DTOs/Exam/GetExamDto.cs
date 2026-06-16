using Project.Domain.Enums;

namespace Project.BLL.DTOs.Exam
{
    public class GetExamDto
    {
        public int Id { get; set; }
        public Guid Uid { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal TotalScore { get; set; }
        public bool IsAIGenerated { get; set; }
        public bool IsPublished { get; set; }
        public EvaluationCategory Category { get; set; }
        public int? ClassSubjectTeacherId { get; set; }
        public int GradeLevelId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string GradeLevelName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int QuestionsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<GetExamQuestionGroupDto> Groups { get; set; } = new();
        public List<GetExamQuestionDto> StandaloneQuestions { get; set; } = new();
    }

    public class GetExamQuestionDto
    {
        public int Id { get; set; }
        public int? GroupId { get; set; }
        public TemplateContentType DisplayType { get; set; }
        public string? ContentText { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Points { get; set; }
        public int DisplayOrder { get; set; }
        public List<GetExamQuestionOptionDto> Options { get; set; } = new();
    }

    public class GetExamQuestionOptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }
    }
}
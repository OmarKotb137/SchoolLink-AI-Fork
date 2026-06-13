using System.Text.Json.Serialization;
using Project.Domain.Enums;

namespace Project.BLL.DTOs.Exam
{
    public class CreateExamFromAiDto
    {
        public int? ClassSubjectTeacherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? DurationMinutes { get; set; }
        public decimal TotalScore { get; set; }
        public EvaluationCategory Category { get; set; }
        public List<AiQuestionGroupDto> Groups { get; set; } = new();
        public List<AiQuestionDto> StandaloneQuestions { get; set; } = new();
    }

    public class AiQuestionGroupDto
    {
        [JsonPropertyName("displayType")]
        public TemplateContentType DisplayType { get; set; }

        [JsonPropertyName("contentTitle")]
        public string? ContentTitle { get; set; }

        [JsonPropertyName("contentText")]
        public string? ContentText { get; set; }

        [JsonPropertyName("imagePrompt")]
        public string? ImagePrompt { get; set; }

        [JsonPropertyName("displayOrder")]
        public int DisplayOrder { get; set; }

        [JsonPropertyName("questions")]
        public List<AiQuestionDto> Questions { get; set; } = new();
    }

    public class AiQuestionDto
    {
        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("questionType")]
        public QuestionType QuestionType { get; set; }

        [JsonPropertyName("options")]
        public List<AiOptionDto>? Options { get; set; }

        [JsonPropertyName("correctAnswer")]
        public string? CorrectAnswer { get; set; }

        [JsonPropertyName("points")]
        public decimal Points { get; set; }

        [JsonPropertyName("displayOrder")]
        public int DisplayOrder { get; set; }
    }

    public class AiOptionDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; set; }

        [JsonPropertyName("displayOrder")]
        public int DisplayOrder { get; set; }
    }
}

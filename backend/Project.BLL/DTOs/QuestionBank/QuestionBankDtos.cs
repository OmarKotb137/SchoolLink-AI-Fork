namespace Project.BLL.DTOs.QuestionBank;

public class QuestionBankItemDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int QuestionType { get; set; }
    public string? CorrectAnswer { get; set; }
    public List<QuestionBankOptionDto> Options { get; set; } = new();
    public string SubjectName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QuestionBankOptionDto
{
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }
}

public class AddToQuestionBankDto
{
    public string QuestionText { get; set; } = string.Empty;
    public int QuestionType { get; set; }
    public string? CorrectAnswer { get; set; }
    public List<AddOptionDto> Options { get; set; } = new();
    public int SubjectId { get; set; }
    public int? SourceExamId { get; set; }
}

public class AddOptionDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }
}

public class SearchQuestionBankDto
{
    public string? SearchText { get; set; }
    public int? SubjectId { get; set; }
    public int? QuestionType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

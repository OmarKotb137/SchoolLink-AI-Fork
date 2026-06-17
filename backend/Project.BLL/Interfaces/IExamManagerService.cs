using Common.Results;

namespace Project.BLL.Interfaces;

public record ExamManagerItemDto(
    int Id, string Name, string Subject, string Class,
    string Date, string StartTime, string EndTime,
    int Duration, int QuestionCount, string Status,
    double? AvgScore, int? Submitted, int? Total,
    bool IsResultPublished
);

public record ExamManagerQuestionDto(
    int Id, string Type, string Text,
    List<string>? Options, string CorrectAnswer
);

public record ExamManagerDetailDto(
    int Id, string Name, string Subject, string Class,
    string Date, string StartTime, string EndTime,
    int Duration, int QuestionCount, string Status,
    bool IsResultPublished,
    List<ExamManagerQuestionDto> Questions
);

public record ExamManagerStatsDto(
    int Total, int Upcoming, int Ended, double AvgScore
);

public record CreateExamManagerDto(
    string Title, int SubjectId, int ClassId,
    string Date, string StartTime, string EndTime,
    int DurationMinutes
);

public interface IExamManagerService
{
    Task<OperationResult<List<ExamManagerItemDto>>> GetAllAsync(int? cstId = null);
    Task<OperationResult<ExamManagerDetailDto>> GetByIdAsync(int id);
    Task<OperationResult<ExamManagerStatsDto>> GetStatsAsync(int? cstId = null);
    Task<OperationResult<ExamManagerDetailDto>> CreateAsync(CreateExamManagerDto dto, int teacherId);
    Task<OperationResult> UpdateAsync(int id, CreateExamManagerDto dto);
    Task<OperationResult> DeleteAsync(int id);
    Task<OperationResult> PublishAsync(int id, int teacherId);
    Task<OperationResult> ToggleResultPublishStatusAsync(int id, bool isPublished, int teacherId);
}

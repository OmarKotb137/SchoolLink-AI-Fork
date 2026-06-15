using Common.Results;
using Project.BLL.DTOs.Assignment;

namespace Project.BLL.Interfaces;

public class AssignmentManagerItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Deadline { get; set; } = string.Empty;
    public int Submitted { get; set; }
    public int Total { get; set; }
    public string Status { get; set; } = "draft";
}

public class AssignmentManagerQuestionDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "mcq";
    public string Text { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
}

public class AssignmentManagerDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Deadline { get; set; } = string.Empty;
    public int Submitted { get; set; }
    public int Total { get; set; }
    public string Status { get; set; } = "draft";
    public List<AssignmentManagerQuestionDto> Questions { get; set; } = new();
}

public class AssignmentManagerStatsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public double AvgDelivery { get; set; }
    public int Overdue { get; set; }
}

public class CreateAssignmentManagerDto
{
    public string Title { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int ClassId { get; set; }
    public string? Deadline { get; set; }
    public List<CreateManagerQuestionDto> Questions { get; set; } = new();
}

public class CreateManagerQuestionDto
{
    public string Type { get; set; } = "mcq";
    public string Text { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
}

public class UpdateAssignmentManagerDto
{
    public string Title { get; set; } = string.Empty;
    public string? Deadline { get; set; }
    public List<CreateManagerQuestionDto> Questions { get; set; } = new();
}

public interface IAssignmentManagerService
{
    Task<OperationResult<List<AssignmentManagerItemDto>>> GetAllAsync(int? classSubjectTeacherId = null);
    Task<OperationResult<AssignmentManagerDetailDto>> GetByIdAsync(int id);
    Task<OperationResult<AssignmentManagerItemDto>> CreateAsync(CreateAssignmentManagerDto dto, int teacherId);
    Task<OperationResult> UpdateAsync(int id, UpdateAssignmentManagerDto dto);
    Task<OperationResult> DeleteAsync(int id);
    Task<OperationResult<AssignmentManagerStatsDto>> GetStatsAsync(int? classSubjectTeacherId = null);
}

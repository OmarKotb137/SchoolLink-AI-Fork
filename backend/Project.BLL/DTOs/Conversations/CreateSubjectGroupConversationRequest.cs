namespace Project.BLL.DTOs.Conversations;

public class CreateSubjectGroupConversationRequest
{
    public int CreatorUserId { get; set; }
    public int SubjectId { get; set; }
    public int ClassId { get; set; }
    public int AcademicYearId { get; set; }
    public string? Title { get; set; }
}

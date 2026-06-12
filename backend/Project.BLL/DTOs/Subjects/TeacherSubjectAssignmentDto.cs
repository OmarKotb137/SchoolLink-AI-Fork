namespace Project.BLL.DTOs;

/// <summary>
/// DTO for AI tools — carries the actual ClassSubjectTeacher.Id
/// that the LLM needs to pass to generate_exam_with_ai and other tools.
/// </summary>
public class TeacherSubjectAssignmentDto
{
    public int    ClassSubjectTeacherId { get; set; }
    public int    SubjectId             { get; set; }
    public string SubjectName           { get; set; } = string.Empty;
    public string ClassName             { get; set; } = string.Empty;
}

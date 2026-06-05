namespace Project.BLL.DTOs.Students;

public class BulkCreateStudentsRequest
{
    public List<CreateStudentRequest> Students { get; set; } = new();
}

using Project.Domain.Enums;

namespace Project.BLL.DTOs.ParentStudents;

public class LinkParentStudentRequest
{
    public int ParentId { get; set; }
    public int StudentId { get; set; }
    public RelationshipType Relationship { get; set; }
}

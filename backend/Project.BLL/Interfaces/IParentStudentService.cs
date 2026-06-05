using Common.Results;
using Project.BLL.DTOs.ParentStudents;
using Project.BLL.DTOs.Students;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IParentStudentService
{
    Task<OperationResult<ParentStudentDto>> LinkParentToStudentAsync(LinkParentStudentRequest request);
    Task<OperationResult> UnlinkParentFromStudentAsync(int parentStudentId);
    Task<OperationResult<IEnumerable<StudentDto>>> GetStudentsByParentAsync(int parentId);
    Task<OperationResult<IEnumerable<ParentStudentDto>>> GetParentsByStudentAsync(int studentId);
    Task<OperationResult<ParentStudentDto>> UpdateRelationshipAsync(int parentStudentId, RelationshipType newRelationship);
    Task<OperationResult<bool>> CheckRelationshipAsync(int parentId, int studentId);
}

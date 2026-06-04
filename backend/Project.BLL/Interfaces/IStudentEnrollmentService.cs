using Common.Results;
using Project.BLL.DTOs.Enrollments;

namespace Project.BLL.Interfaces;

public interface IStudentEnrollmentService
{
    Task<OperationResult<EnrollmentDto>> EnrollStudentAsync(EnrollStudentRequest request);
    Task<OperationResult<EnrollmentDto>> TransferStudentAsync(TransferStudentRequest request);
    Task<OperationResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsByStudentAsync(int studentId);
    Task<OperationResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsByClassAsync(int classId, int academicYearId, bool activeOnly);
    Task<OperationResult<EnrollmentDto>> GetActiveEnrollmentByStudentAsync(int studentId, int academicYearId);
    Task<OperationResult<BulkEnrollResultDto>> BulkEnrollStudentsAsync(BulkEnrollStudentsRequest request);
}

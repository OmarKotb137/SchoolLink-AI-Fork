using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Enrollments;

namespace Project.BLL.Interfaces;

public interface IStudentEnrollmentService
{
    Task<OperationResult<EnrollmentDto>> EnrollStudentAsync(EnrollStudentRequest request);
    Task<OperationResult<EnrollmentDto>> TransferStudentAsync(TransferStudentRequest request);
    Task<OperationResult<BulkTransferResultDto>> BulkTransferStudentsAsync(BulkTransferStudentsRequest request);
    Task<OperationResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsByStudentAsync(int studentId);
    Task<OperationResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsByClassAsync(int classId, int academicYearId, bool activeOnly);
    Task<OperationResult<PagedResult<EnrollmentDto>>> GetEnrollmentsByClassPagedAsync(int classId, int academicYearId, int page, int pageSize, bool activeOnly = true, string? searchTerm = null);
    Task<OperationResult<EnrollmentDto>> GetActiveEnrollmentByStudentAsync(int studentId, int academicYearId);
    Task<OperationResult<PagedResult<TransferHistoryDto>>> GetTransferHistoryAsync(int academicYearId, int page = 1, int pageSize = 20);
    Task<OperationResult<BulkEnrollResultDto>> BulkEnrollStudentsAsync(BulkEnrollStudentsRequest request);
}

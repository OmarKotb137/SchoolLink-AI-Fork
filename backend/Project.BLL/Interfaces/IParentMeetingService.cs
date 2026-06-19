using Common.Results;
using Project.BLL.DTOs.Meetings;
using Project.Domain.Entities;

namespace Project.BLL.Interfaces;

public interface IParentMeetingService
{
    Task<OperationResult<ParentMeetingRequestDto>> CreateRequestAsync(CreateMeetingRequest request);
    Task<OperationResult<ParentMeetingRequestDto>> ApproveRequestAsync(int requestId, int adminId, DateTime scheduledDate);
    Task<OperationResult<ParentMeetingRequestDto>> RejectRequestAsync(int requestId, int adminId, string? reason);
    Task<OperationResult<ParentMeetingRequestDto>> CompleteRequestAsync(int requestId, int userId);
    Task<OperationResult<IEnumerable<ParentMeetingRequestDto>>> GetRequestsByParentAsync(int parentId);
    Task<OperationResult<IEnumerable<ParentMeetingRequestDto>>> GetRequestsByTeacherAsync(int teacherId);
    Task<OperationResult<IEnumerable<ParentMeetingRequestDto>>> GetAllRequestsAsync();
    Task<OperationResult<ParentMeetingRequestDto>> GetRequestByIdAsync(int requestId);
}

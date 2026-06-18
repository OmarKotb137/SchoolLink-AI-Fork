using Common.Results;
using Project.BLL.DTOs.Meetings;
using Project.Domain.Entities;

namespace Project.BLL.Interfaces;

public interface IParentMeetingService
{
    Task<OperationResult<ParentMeetingRequestDto>> CreateRequestAsync(CreateMeetingRequest request);
    Task<OperationResult<ParentMeetingRequestDto>> ApproveRequestAsync(int requestId, int teacherId, DateTime scheduledDate);
    Task<OperationResult<ParentMeetingRequestDto>> RejectRequestAsync(int requestId, int teacherId, string? reason);
    Task<OperationResult<ParentMeetingRequestDto>> CompleteRequestAsync(int requestId, int userId);
    Task<OperationResult<IEnumerable<ParentMeetingRequestDto>>> GetRequestsByParentAsync(int parentId);
    Task<OperationResult<IEnumerable<ParentMeetingRequestDto>>> GetRequestsByTeacherAsync(int teacherId);
    Task<OperationResult<ParentMeetingRequestDto>> GetRequestByIdAsync(int requestId);
}

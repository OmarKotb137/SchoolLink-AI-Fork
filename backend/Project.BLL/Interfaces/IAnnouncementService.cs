using Common.Results;
using Project.BLL.DTOs.Announcements;
using Project.BLL.DTOs.Common;

namespace Project.BLL.Interfaces;

public interface IAnnouncementService
{
    Task<OperationResult<AnnouncementDto>> CreateAnnouncementAsync(CreateAnnouncementRequest request);
    Task<OperationResult<AnnouncementDto>> GetAnnouncementByIdAsync(int id);
    Task<OperationResult<IEnumerable<AnnouncementDto>>> GetActiveAnnouncementsAsync(GetAnnouncementsFilter filter);
    Task<OperationResult<IEnumerable<AnnouncementDto>>> GetExpiredAnnouncementsAsync(int callerUserId);
    Task<OperationResult<AnnouncementDto>> UpdateAnnouncementAsync(int id, CreateAnnouncementRequest request);
    Task<OperationResult> DeleteAnnouncementAsync(int id, int callerUserId);
    Task<OperationResult<IEnumerable<AnnouncementDto>>> SearchAnnouncementsAsync(string term);
    Task<OperationResult<PagedResult<AnnouncementDto>>> GetAnnouncementsByAuthorAsync(int authorId, PaginationFilter filter);
    Task<OperationResult> CleanupExpiredAnnouncementsAsync(int callerUserId);
}

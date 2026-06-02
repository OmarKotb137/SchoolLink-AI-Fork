using Common.Results;
using Project.BLL.DTOs.Announcements;

namespace Project.BLL.Interfaces;

public interface IAnnouncementService
{
    Task<OperationResult<AnnouncementDto>> CreateAnnouncementAsync(CreateAnnouncementRequest request);
    Task<OperationResult<AnnouncementDto>> GetAnnouncementByIdAsync(int id);
    Task<OperationResult<IEnumerable<AnnouncementDto>>> GetActiveAnnouncementsAsync(GetAnnouncementsFilter filter);
    Task<OperationResult<IEnumerable<AnnouncementDto>>> GetExpiredAnnouncementsAsync(int callerUserId);
    Task<OperationResult<AnnouncementDto>> UpdateAnnouncementAsync(int id, CreateAnnouncementRequest request);
    Task<OperationResult> DeleteAnnouncementAsync(int id, int callerUserId);
}

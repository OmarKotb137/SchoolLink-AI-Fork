using Common.Results;
using Project.BLL.DTOs.Announcements;

namespace Project.BLL.Interfaces;

public interface IAnnouncementService
{
    Task<OperationResult<AnnouncementDto>> CreateAnnouncementAsync(CreateAnnouncementRequest request);
    Task<OperationResult<IEnumerable<AnnouncementDto>>> GetActiveAnnouncementsAsync(GetAnnouncementsFilter filter);
}

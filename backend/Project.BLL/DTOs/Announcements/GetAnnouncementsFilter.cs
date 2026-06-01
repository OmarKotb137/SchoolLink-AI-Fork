using SchoolLink.Domain.Enums;

namespace Project.BLL.DTOs.Announcements;

public class GetAnnouncementsFilter
{
    public int CallerUserId { get; set; }
    public UserRole CallerRole { get; set; }
    public int? ClassId { get; set; }
}

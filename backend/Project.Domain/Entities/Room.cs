using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class Room : BaseEntity
    {
        public string   Name     { get; set; } = string.Empty;
        public RoomType Type     { get; set; }
        public int?     Capacity { get; set; }

        // Navigation
        public ICollection<TimetableSlot> TimetableSlots { get; set; } = new List<TimetableSlot>();
    }
}

using SchoolLink.Domain.Enums;

namespace SchoolLink.Domain.Entities
{
    public class TimetableSlot : BaseEntity
    {
        public int      TimetableId           { get; set; }
        public SchoolDay DayOfWeek            { get; set; }
        public int      PeriodNumber          { get; set; }
        public TimeOnly StartTime             { get; set; }
        public TimeOnly EndTime               { get; set; }
        public int?     ClassSubjectTeacherId { get; set; }
        public bool     IsBreak               { get; set; } = false;
        public int?     RoomId                { get; set; }

        // Navigation Properties
        public Timetable            Timetable            { get; set; } = null!;
        public ClassSubjectTeacher? ClassSubjectTeacher  { get; set; }
        public Room?                Room                 { get; set; }
    }
}

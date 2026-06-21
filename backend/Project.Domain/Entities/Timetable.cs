namespace Project.Domain.Entities
{
    public class Timetable : BaseEntity
    {
        public int ClassId { get; set; }
        public int AcademicYearId { get; set; }
        // الجداول تُنشأ دائمًا كمسودة (غير منشورة). التفعيل يتم فقط عبر ActivateTimetableAsync
        // بعد اجتياز المراجعة. الافتراضي القديم `true` كان يسمح بنشر جدول فارغ عن طريق الخطأ.
        public bool IsActive { get; set; } = false;

        // Navigation Properties
        public SchoolClass Class { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public ICollection<TimetableSlot> Slots { get; set; } = new List<TimetableSlot>();
    }
}

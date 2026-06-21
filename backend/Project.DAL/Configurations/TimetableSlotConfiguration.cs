using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class TimetableSlotConfiguration : IEntityTypeConfiguration<TimetableSlot>
    {
        public void Configure(EntityTypeBuilder<TimetableSlot> builder)
        {
            builder.HasKey(x => x.Id);

            // حصة واحدة فقط لكل (جدول، يوم، رقم حصة) — قاعدة الخلية الواحدة لكل فصل.
            // هذا هو القيد الفعلي الوحيد على مستوى الـ DB: داخل الجدول الواحد (فصل واحد)،
            // لا يمكن وجود حجزين في نفس (يوم، حصة). تعارض القاعة/المعلم عبر الفصول المختلفة
            // هو Business Logic يُفحص في طبقة الـ application لأن الـ DB لا تفرق بين المسودة
            // والجدول المنشور (كلاهما يحتوي نفس القاعات عند النسخ من المنشور لمسودة جديدة).
            builder.HasIndex(x => new { x.TimetableId, x.DayOfWeek, x.PeriodNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.Property(x => x.StartTime)
                .HasColumnType("time");

            builder.Property(x => x.EndTime)
                .HasColumnType("time");

            builder.HasOne(x => x.Timetable)
                .WithMany(x => x.Slots)
                .HasForeignKey(x => x.TimetableId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ClassSubjectTeacher)
                .WithMany()
                .HasForeignKey(x => x.ClassSubjectTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Room)
                .WithMany(r => r.TimetableSlots)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

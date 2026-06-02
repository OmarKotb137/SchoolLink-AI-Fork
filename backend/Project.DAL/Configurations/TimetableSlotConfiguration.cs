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

            // حصة واحدة فقط لكل (جدول، يوم، رقم حصة)
            builder.HasIndex(x => new { x.TimetableId, x.DayOfWeek, x.PeriodNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // غرفة واحدة فقط لكل (غرفة، يوم، رقم حصة) — تعارض الغرف
            builder.HasIndex(x => new { x.RoomId, x.DayOfWeek, x.PeriodNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [RoomId] IS NOT NULL");

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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class TimetableSlotConfiguration : IEntityTypeConfiguration<TimetableSlot>
    {
        public void Configure(EntityTypeBuilder<TimetableSlot> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.TimetableId, x.DayOfWeek, x.PeriodNumber })
                .IsUnique();

            builder.HasOne(x => x.Timetable)
                .WithMany(x => x.Slots)
                .HasForeignKey(x => x.TimetableId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ClassSubjectTeacher)
                .WithMany()
                .HasForeignKey(x => x.ClassSubjectTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
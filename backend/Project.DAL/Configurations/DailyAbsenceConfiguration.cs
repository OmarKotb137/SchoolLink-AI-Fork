using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class DailyAbsenceConfiguration : IEntityTypeConfiguration<DailyAbsence>
    {
        public void Configure(EntityTypeBuilder<DailyAbsence> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Reason)
                .HasMaxLength(300);

            builder.Property(x => x.AbsenceDate)
                .HasColumnType("date");

            builder.HasIndex(x => new { x.EnrollmentId, x.ClassSubjectTeacherId, x.AbsenceDate })
                .IsUnique();

            builder.HasOne(x => x.Enrollment)
                .WithMany()
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ClassSubjectTeacher)
                .WithMany()
                .HasForeignKey(x => x.ClassSubjectTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Period)
                .WithMany()
                .HasForeignKey(x => x.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.RecordedBy)
                .WithMany()
                .HasForeignKey(x => x.RecordedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

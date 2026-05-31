using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class LessonFeedbackConfiguration : IEntityTypeConfiguration<LessonFeedback>
    {
        public void Configure(EntityTypeBuilder<LessonFeedback> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Comment)
                .HasMaxLength(1000);

            builder.Property(x => x.LessonDate)
                .HasColumnType("date");

            builder.HasOne(x => x.Enrollment)
                .WithMany()
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ClassSubjectTeacher)
                .WithMany()
                .HasForeignKey(x => x.ClassSubjectTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

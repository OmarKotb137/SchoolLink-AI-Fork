using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class StudentExamAttemptConfiguration : IEntityTypeConfiguration<StudentExamAttempt>
    {
        public void Configure(EntityTypeBuilder<StudentExamAttempt> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Score)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.TotalScore)
                .HasColumnType("decimal(5,2)");

            builder.HasOne(x => x.Enrollment)
                .WithMany()
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Exam)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

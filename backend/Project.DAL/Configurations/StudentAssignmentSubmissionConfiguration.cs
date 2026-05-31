using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class StudentAssignmentSubmissionConfiguration : IEntityTypeConfiguration<StudentAssignmentSubmission>
    {
        public void Configure(EntityTypeBuilder<StudentAssignmentSubmission> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Score)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.MaxScore)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.AIFeedback)
                .HasMaxLength(3000);

            builder.HasIndex(x => new { x.EnrollmentId, x.AssignmentId })
                .IsUnique();

            builder.HasOne(x => x.Enrollment)
                .WithMany()
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Assignment)
                .WithMany(x => x.Submissions)
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
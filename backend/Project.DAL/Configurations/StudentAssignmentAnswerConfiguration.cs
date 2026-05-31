using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class StudentAssignmentAnswerConfiguration : IEntityTypeConfiguration<StudentAssignmentAnswer>
    {
        public void Configure(EntityTypeBuilder<StudentAssignmentAnswer> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AnswerText)
                .HasMaxLength(5000);

            builder.Property(x => x.PointsEarned)
                .HasColumnType("decimal(4,2)");

            builder.Property(x => x.AIFeedback)
                .HasMaxLength(2000);

            builder.HasIndex(x => new { x.SubmissionId, x.QuestionId })
                .IsUnique();

            builder.HasOne(x => x.Submission)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SelectedOption)
                .WithMany()
                .HasForeignKey(x => x.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

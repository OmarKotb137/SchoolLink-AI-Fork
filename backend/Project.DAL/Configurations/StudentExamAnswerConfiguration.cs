using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class StudentExamAnswerConfiguration : IEntityTypeConfiguration<StudentExamAnswer>
    {
        public void Configure(EntityTypeBuilder<StudentExamAnswer> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AnswerText)
                .HasMaxLength(5000);

            builder.Property(x => x.PointsEarned)
                .HasColumnType("decimal(4,2)");

            builder.Property(x => x.AIFeedback)
                .HasMaxLength(2000);

            builder.HasIndex(x => new { x.AttemptId, x.QuestionId })
                .IsUnique();

            builder.HasOne(x => x.Attempt)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.AttemptId)
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

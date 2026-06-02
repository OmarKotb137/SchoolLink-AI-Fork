using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
    {
        public void Configure(EntityTypeBuilder<ExamQuestion> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuestionText)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.CorrectAnswer)
                .HasMaxLength(1000);

            builder.Property(x => x.ImageUrl)
                .HasMaxLength(500);

            builder.Property(x => x.Points)
                .HasColumnType("decimal(4,2)");

            builder.HasOne(x => x.Exam)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

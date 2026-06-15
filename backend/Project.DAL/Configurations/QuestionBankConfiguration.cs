using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class QuestionBankConfiguration : IEntityTypeConfiguration<QuestionBank>
    {
        public void Configure(EntityTypeBuilder<QuestionBank> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuestionText)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.CorrectAnswer)
                .HasMaxLength(1000);

            builder.Property(x => x.OptionsJson)
                .HasColumnType("nvarchar(max)");

            builder.HasOne(x => x.Subject)
                .WithMany()
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.GradeLevel)
                .WithMany()
                .HasForeignKey(x => x.GradeLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SourceExam)
                .WithMany()
                .HasForeignKey(x => x.SourceExamId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

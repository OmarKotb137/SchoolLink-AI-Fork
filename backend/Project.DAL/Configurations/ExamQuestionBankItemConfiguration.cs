using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class ExamQuestionBankItemConfiguration : IEntityTypeConfiguration<ExamQuestionBankItem>
    {
        public void Configure(EntityTypeBuilder<ExamQuestionBankItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Points)
                .HasColumnType("decimal(4,2)");

            builder.HasOne(x => x.Exam)
                .WithMany(x => x.QuestionBankLinks)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.QuestionBank)
                .WithMany(x => x.ExamLinks)
                .HasForeignKey(x => x.QuestionBankId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ExamId, x.QuestionBankId }).IsUnique();

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

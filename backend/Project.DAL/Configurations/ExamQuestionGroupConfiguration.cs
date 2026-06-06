using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class ExamQuestionGroupConfiguration : IEntityTypeConfiguration<ExamQuestionGroup>
    {
        public void Configure(EntityTypeBuilder<ExamQuestionGroup> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ContentTitle)
                .HasMaxLength(300);

            builder.Property(x => x.ContentText)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.ImagePrompt)
                .HasMaxLength(1000);

            builder.Property(x => x.ImageUrl)
                .HasMaxLength(500);

            builder.HasOne(x => x.Exam)
                .WithMany(x => x.Groups)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Questions)
                .WithOne(x => x.Group)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

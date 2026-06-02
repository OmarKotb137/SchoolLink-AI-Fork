using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class AssignmentQuestionOptionConfiguration : IEntityTypeConfiguration<AssignmentQuestionOption>
    {
        public void Configure(EntityTypeBuilder<AssignmentQuestionOption> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OptionText)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasOne(x => x.Question)
                .WithMany(x => x.Options)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

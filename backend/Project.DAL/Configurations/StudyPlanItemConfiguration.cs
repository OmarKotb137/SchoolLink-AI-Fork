using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class StudyPlanItemConfiguration : IEntityTypeConfiguration<StudyPlanItem>
    {
        public void Configure(EntityTypeBuilder<StudyPlanItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Topic)
                .HasMaxLength(300);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasOne(x => x.StudyPlan)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.StudyPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Subject)
                .WithMany()
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
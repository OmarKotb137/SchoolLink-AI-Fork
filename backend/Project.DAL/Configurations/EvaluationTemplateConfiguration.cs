using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class EvaluationTemplateConfiguration : IEntityTypeConfiguration<EvaluationTemplate>
    {
        public void Configure(EntityTypeBuilder<EvaluationTemplate> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(x => new { x.GradeLevelId, x.SubjectId, x.AcademicYearId })
                .IsUnique();

            builder.HasOne(x => x.GradeLevel)
                .WithMany()
                .HasForeignKey(x => x.GradeLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Subject)
                .WithMany()
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AcademicYear)
                .WithMany()
                .HasForeignKey(x => x.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

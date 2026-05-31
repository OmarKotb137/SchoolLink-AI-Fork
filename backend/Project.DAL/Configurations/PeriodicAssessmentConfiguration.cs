using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class PeriodicAssessmentConfiguration : IEntityTypeConfiguration<PeriodicAssessment>
    {
        public void Configure(EntityTypeBuilder<PeriodicAssessment> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Score)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.MaxScore)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.AssessmentDate)
                .HasColumnType("date");

            builder.HasIndex(x => new { x.EnrollmentId, x.AssessmentType })
                .IsUnique();

            builder.HasOne(x => x.Enrollment)
                .WithMany()
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

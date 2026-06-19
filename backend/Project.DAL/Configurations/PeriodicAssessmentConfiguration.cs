using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

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

            builder.Property(x => x.Term)
                .HasConversion<int>();

            builder.HasIndex(x => new { x.EnrollmentId, x.SubjectId, x.AssessmentType, x.Term })
                .IsUnique();

            builder.HasOne(x => x.Enrollment)
                .WithMany()
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Subject)
                .WithMany()
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

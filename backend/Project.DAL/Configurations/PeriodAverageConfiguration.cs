using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class PeriodAverageConfiguration : IEntityTypeConfiguration<PeriodAverage>
    {
        public void Configure(EntityTypeBuilder<PeriodAverage> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AvgScore)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.MaxScore)
                .HasColumnType("decimal(5,2)");

            builder.HasIndex(x => new { x.EnrollmentId, x.PeriodId })
                .IsUnique();

            builder.HasOne(x => x.Enrollment)
                .WithMany()
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Period)
                .WithMany()
                .HasForeignKey(x => x.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
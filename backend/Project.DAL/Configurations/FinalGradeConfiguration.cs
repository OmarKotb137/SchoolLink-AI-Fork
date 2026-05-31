using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class FinalGradeConfiguration : IEntityTypeConfiguration<FinalGrade>
    {
        public void Configure(EntityTypeBuilder<FinalGrade> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PeriodAvgScore)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.Assessment1Score)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.Assessment2Score)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.WrittenTotal)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.FinalExamScore)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.Total)
                .HasColumnType("decimal(5,2)");

            builder.HasIndex(x => x.EnrollmentId)
                .IsUnique();

            builder.HasOne(x => x.Enrollment)
                .WithMany()
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

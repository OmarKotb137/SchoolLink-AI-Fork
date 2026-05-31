using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class StudentEvaluationConfiguration : IEntityTypeConfiguration<StudentEvaluation>
    {
        public void Configure(EntityTypeBuilder<StudentEvaluation> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Score)
                .HasColumnType("decimal(5,2)");

            builder.HasIndex(x => new { x.EnrollmentId, x.EvaluationItemId, x.PeriodId })
                .IsUnique();

            builder.HasOne(x => x.Enrollment)
                .WithMany()
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.EvaluationItem)
                .WithMany()
                .HasForeignKey(x => x.EvaluationItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Period)
                .WithMany()
                .HasForeignKey(x => x.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.EnteredBy)
                .WithMany()
                .HasForeignKey(x => x.EnteredById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
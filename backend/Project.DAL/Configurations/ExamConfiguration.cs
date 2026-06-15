using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class ExamConfiguration : IEntityTypeConfiguration<Exam>
    {
        public void Configure(EntityTypeBuilder<Exam> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(300);

            builder.HasIndex(x => x.Uid).IsUnique();

            builder.Property(x => x.Uid)
                .IsRequired()
                .HasDefaultValueSql("NEWID()");

            builder.Property(x => x.TotalScore)
                .HasColumnType("decimal(5,2)");

            builder.HasOne(x => x.ClassSubjectTeacher)
                .WithMany()
                .HasForeignKey(x => x.ClassSubjectTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Subject)
                .WithMany(s => s.Exams)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.GradeLevel)
                .WithMany()
                .HasForeignKey(x => x.GradeLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

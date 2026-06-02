using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class TimetableConfiguration : IEntityTypeConfiguration<Timetable>
    {
        public void Configure(EntityTypeBuilder<Timetable> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Class)
                .WithMany()
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AcademicYear)
                .WithMany()
                .HasForeignKey(x => x.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

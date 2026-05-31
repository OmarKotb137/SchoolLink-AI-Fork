using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class ResultVisibilitySettingConfiguration : IEntityTypeConfiguration<ResultVisibilitySetting>
    {
        public void Configure(EntityTypeBuilder<ResultVisibilitySetting> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.AcademicYearId, x.Term })
                .IsUnique();

            builder.HasOne(x => x.AcademicYear)
                .WithMany()
                .HasForeignKey(x => x.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ControlledBy)
                .WithMany()
                .HasForeignKey(x => x.ControlledById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

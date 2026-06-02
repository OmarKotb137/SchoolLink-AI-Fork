using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class SchoolProfileConfiguration : IEntityTypeConfiguration<SchoolProfile>
    {
        public void Configure(EntityTypeBuilder<SchoolProfile> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SchoolName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Governorate)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Directorate)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.EducationalAdministration)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Address)
                .HasMaxLength(300);

            builder.Property(x => x.Phone)
                .HasMaxLength(30);

            builder.Property(x => x.Email)
                .HasMaxLength(150);

            builder.Property(x => x.ManagerName)
                .HasMaxLength(150);

            builder.Property(x => x.LogoPath)
                .HasMaxLength(500);

            builder.HasIndex(x => x.IsActive);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

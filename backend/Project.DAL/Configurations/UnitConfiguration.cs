using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class UnitConfiguration : IEntityTypeConfiguration<Unit>
    {
        public void Configure(EntityTypeBuilder<Unit> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.DisplayOrder)
                .HasDefaultValue(0);

builder.HasOne(x => x.Subject)
    .WithMany(s => s.Units)
    .HasForeignKey(x => x.SubjectId)
    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Lessons)
                .WithOne(l => l.Unit)
                .HasForeignKey(l => l.UnitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

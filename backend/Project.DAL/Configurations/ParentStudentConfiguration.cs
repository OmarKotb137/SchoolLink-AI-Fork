using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class ParentStudentConfiguration : IEntityTypeConfiguration<ParentStudent>
    {
        public void Configure(EntityTypeBuilder<ParentStudent> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.ParentId, x.StudentId })
                .IsUnique();

            builder.HasOne(x => x.Parent)
                .WithMany()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Student)
                .WithMany(x => x.Parents)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

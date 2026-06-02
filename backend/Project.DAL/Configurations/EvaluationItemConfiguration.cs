using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class EvaluationItemConfiguration : IEntityTypeConfiguration<EvaluationItem>
    {
        public void Configure(EntityTypeBuilder<EvaluationItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.MaxScore)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.Weight)
                .HasColumnType("decimal(4,2)");

            builder.HasOne(x => x.Template)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolLink.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class AIGenerationLogConfiguration : IEntityTypeConfiguration<AIGenerationLog>
    {
        public void Configure(EntityTypeBuilder<AIGenerationLog> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OperationType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.InputSummary)
                .HasMaxLength(1000);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
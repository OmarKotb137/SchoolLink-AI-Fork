using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(x => x.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username");

            builder.Property(x => x.ContactEmail)
                .HasMaxLength(200);

            builder.Property(x => x.Phone)
                .HasMaxLength(20);

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.ProfilePictureUrl)
                .HasMaxLength(500);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

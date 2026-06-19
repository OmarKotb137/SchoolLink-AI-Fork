using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class AnnouncementUserConfiguration : IEntityTypeConfiguration<AnnouncementUser>
    {
        public void Configure(EntityTypeBuilder<AnnouncementUser> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Announcement)
                .WithMany(a => a.AnnouncementUsers)
                .HasForeignKey(x => x.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.AnnouncementId, x.UserId }).IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

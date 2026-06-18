using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class ParentMeetingRequestConfiguration : IEntityTypeConfiguration<ParentMeetingRequest>
    {
        public void Configure(EntityTypeBuilder<ParentMeetingRequest> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Reason)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.Notes)
                .HasMaxLength(2000);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasDefaultValue(MeetingRequestStatus.Pending);

            builder.HasOne(x => x.Parent)
                .WithMany()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.HandledBy)
                .WithMany()
                .HasForeignKey(x => x.HandledById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}

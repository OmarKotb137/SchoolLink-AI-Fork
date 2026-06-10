using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Data.Configurations;

public class BlockedUserConfiguration : IEntityTypeConfiguration<BlockedUser>
{
    public void Configure(EntityTypeBuilder<BlockedUser> builder)
    {
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.BlockerId, b.BlockedUserId, b.ConversationId }).IsUnique();
        builder.HasQueryFilter(b => !b.IsDeleted);
        builder.HasOne(b => b.Blocker).WithMany().HasForeignKey(b => b.BlockerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Blocked).WithMany().HasForeignKey(b => b.BlockedUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Conversation).WithMany().HasForeignKey(b => b.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}

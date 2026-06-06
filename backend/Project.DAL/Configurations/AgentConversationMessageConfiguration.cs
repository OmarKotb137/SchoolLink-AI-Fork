using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations;

public class AgentConversationMessageConfiguration : IEntityTypeConfiguration<AgentConversationMessage>
{
    public void Configure(EntityTypeBuilder<AgentConversationMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConversationId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Sender).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(8000);
        builder.Property(x => x.AgentType).IsRequired().HasMaxLength(50);

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => new { x.ConversationId, x.Timestamp });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

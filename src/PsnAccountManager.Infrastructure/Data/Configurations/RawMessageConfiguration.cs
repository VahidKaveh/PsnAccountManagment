using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class RawMessageConfiguration : IEntityTypeConfiguration<RawMessage>
{
    public void Configure(EntityTypeBuilder<RawMessage> builder)
    {
        builder.ToTable("RawMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MessageText).IsRequired().HasColumnType("TEXT");
        builder.Property(m => m.ReceivedAt).IsRequired();
        builder.Property(m => m.Status).IsRequired().HasConversion<string>();

        // A message cannot be duplicated from the same channel
        builder.HasIndex(m => new { m.ChannelId, m.ExternalMessageId }).IsUnique();

        builder.HasOne(m => m.Channel)
            .WithMany(c => c.RawMessages)
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("Channels");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("channel_id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.ExternalId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.LastScrapedAt)
            .HasColumnName("last_scraped_at");


        builder.Property(c => c.LastScrapedMessageId)
            .HasColumnName("last_scraped_message_id");
        builder.HasOne(c => c.ParsingProfile)
            .WithMany(p => p.Channels)
            .HasForeignKey(c => c.ParsingProfileId)
            .OnDelete(DeleteBehavior.SetNull); // If a profile is deleted, don't delete the channel
        builder.Property(c => c.TelegramFetchMode)
            .IsRequired()
            .HasConversion<string>() // Store enum as a string
            .HasMaxLength(50)
            .HasDefaultValue(TelegramFetchMode.SinceLastMessage);

        builder.Property(c => c.FetchValue)
            .IsRequired(false); // This value is nullable

        builder.Property(c => c.DelayAfterScrapeMs)
            .IsRequired()
            .HasDefaultValue(1000);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class RawMessageConfiguration : IEntityTypeConfiguration<RawMessage>
{
    public void Configure(EntityTypeBuilder<RawMessage> builder)
    {
        builder.ToTable("RawMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MessageText)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(m => m.ReceivedAt)
            .IsRequired();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>();



        builder.Property(m => m.ProcessedAt)
            .IsRequired(false); // Nullable


        builder.Property(m => m.ProcessingResult)
            .IsRequired(false)
            .HasMaxLength(500);


        builder.Property(m => m.AccountId)
            .IsRequired(false); // Nullable

        // --- Indexes ---

        // A message cannot be duplicated from the same channel
        builder.HasIndex(m => new { m.ChannelId, m.ExternalMessageId })
            .IsUnique();

        // NEW: Index on Status for filtering by processing status
        builder.HasIndex(m => m.Status);

        // NEW: Index on ProcessedAt for filtering recently processed messages
        builder.HasIndex(m => m.ProcessedAt);

        // NEW: Index on AccountId for tracking created accounts
        builder.HasIndex(m => m.AccountId);

        // --- Relationships ---

        builder.HasOne(m => m.Channel)
            .WithMany(c => c.RawMessages)
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        // NEW: Relationship to Account (optional)
        builder.HasOne(m => m.Account)
            .WithMany() // No navigation property on Account side to avoid circular reference
            .HasForeignKey(m => m.AccountId)
            .OnDelete(DeleteBehavior.SetNull); // If Account deleted, keep message but set FK to null
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(a => a.Id);

        // --- Column Mappings and Constraints ---
        builder.Property(a => a.ChannelId).IsRequired();

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(500);

        // Use a column type that supports long text for the full post description
        builder.Property(a => a.Description)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(a => a.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        // Configure decimal properties for pricing
        builder.Property(a => a.PricePs4)
            .HasColumnType("decimal(18, 2)"); // Nullable

        builder.Property(a => a.PricePs5)
            .HasColumnType("decimal(18, 2)"); // Nullable

        builder.Property(a => a.Region)
            .HasMaxLength(100);

        builder.Property(a => a.HasOriginalMail)
            .IsRequired();

        builder.Property(a => a.GuaranteeMinutes); // Nullable int

        builder.Property(a => a.SellerInfo)
            .HasMaxLength(100);

        // Store enums as strings for readability
        builder.Property(a => a.Capacity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.StockStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.LastScrapedAt)
            .IsRequired();


        builder.Property(a => a.ProcessedAt)
            .IsRequired(false); // Nullable

        builder.Property(a => a.ProcessingResult)
            .IsRequired(false)
            .HasMaxLength(500);


        builder.Property(a => a.RawMessageId)
            .IsRequired(false); // Nullable

        // --- Indexes ---

        // A unique constraint to prevent duplicating the same message from the same channel
        builder.HasIndex(a => new { a.ChannelId, a.ExternalId })
            .IsUnique();

        // NEW: Index on ProcessedAt for filtering recently processed accounts
        builder.HasIndex(a => a.ProcessedAt);

        // NEW: Index on RawMessageId for tracking source message
        builder.HasIndex(a => a.RawMessageId);

        // --- Relationships ---

        builder.HasOne(a => a.Channel)
            .WithMany(c => c.Accounts)
            .HasForeignKey(a => a.ChannelId)
            .OnDelete(DeleteBehavior.Cascade); // If a channel is deleted, its accounts are also deleted

        // NEW: Relationship to RawMessage (optional)
        builder.HasOne(a => a.RawMessage)
            .WithOne() // No navigation property on RawMessage side to avoid circular reference
            .HasForeignKey<Account>(a => a.RawMessageId)
            .OnDelete(DeleteBehavior.SetNull); // If RawMessage deleted, keep Account but set FK to null
    }
}
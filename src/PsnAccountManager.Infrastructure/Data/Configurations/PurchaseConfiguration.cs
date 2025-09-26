using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");
        builder.HasKey(p => p.Id);

        // --- Column Mappings and Constraints ---
        builder.Property(p => p.BuyerUserId).IsRequired();
        builder.Property(p => p.SellerChannelId).IsRequired();
        builder.Property(p => p.AccountId).IsRequired();

        builder.Property(p => p.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18, 2)");

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            // Using a compatible default value function for MySQL 8+
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // --- Relationships ---
        builder.HasOne(p => p.Buyer)
            .WithMany(u => u.Purchases)
            .HasForeignKey(p => p.BuyerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SellerChannel)
            .WithMany(c => c.Sales)
            .HasForeignKey(p => p.SellerChannelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Account)
            .WithMany(a => a.Purchases)
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
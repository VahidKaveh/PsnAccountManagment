using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("payment_id");

        builder.Property(p => p.PurchaseId).HasColumnName("purchase_id").IsRequired();
        builder.Property(p => p.Amount).HasColumnName("amount").IsRequired().HasColumnType("decimal(18,2)");

        builder.Property(p => p.Provider)
            .HasColumnName("provider")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Status).HasColumnName("status").IsRequired().HasConversion<string>();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");

        builder.HasOne(p => p.Purchase)
            .WithMany(pu => pu.Payments)
            .HasForeignKey(p => p.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade); // A payment cannot exist without a purchase
    }
}
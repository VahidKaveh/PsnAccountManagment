using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("Disputes");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("dispute_id");

        builder.Property(d => d.PurchaseId).HasColumnName("purchase_id").IsRequired();
        builder.Property(d => d.RaisedByUserId).HasColumnName("raised_by").IsRequired();
        builder.Property(d => d.Reason).HasColumnName("reason").IsRequired();
        builder.Property(d => d.Status).HasColumnName("status").IsRequired().HasConversion<string>();
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");

        builder.HasOne(d => d.Purchase)
            .WithMany(p => p.Disputes)
            .HasForeignKey(d => d.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.RaisedBy)
            .WithMany() // No navigation property back from User
            .HasForeignKey(d => d.RaisedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // Do not delete a user if they have disputes
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("Wishlist");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("wishlist_id");

        builder.Property(w => w.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(w => w.GameId).HasColumnName("game_id").IsRequired();

        // A user can't have the same game in their wishlist twice
        builder.HasIndex(w => new { w.UserId, w.GameId }).IsUnique();

        builder.HasOne(w => w.User)
            .WithMany(u => u.Wishlists)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade); // If user is deleted, their wishlist is gone

        builder.HasOne(w => w.Game)
            .WithMany() // No navigation property back from Game
            .HasForeignKey(w => w.GameId)
            .OnDelete(DeleteBehavior.Cascade); // If game is deleted, remove from wishlist
    }
}
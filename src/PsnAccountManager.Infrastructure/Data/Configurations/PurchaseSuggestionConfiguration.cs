using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class PurchaseSuggestionConfiguration : IEntityTypeConfiguration<PurchaseSuggestion>
{
    public void Configure(EntityTypeBuilder<PurchaseSuggestion> builder)
    {
        builder.ToTable("PurchaseSuggestions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("suggestion_id");

        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.RequestId).HasColumnName("request_id"); // Nullable
        builder.Property(s => s.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(s => s.MatchedGames).HasColumnName("matched_games").HasMaxLength(1000);
        builder.Property(s => s.Rank).HasColumnName("rank").IsRequired();

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Request)
            .WithMany(r => r.Suggestions)
            .HasForeignKey(s => s.RequestId)
            .OnDelete(DeleteBehavior.SetNull); // If request is deleted, don't delete suggestion

        builder.HasOne(s => s.Account)
            .WithMany()
            .HasForeignKey(s => s.AccountId)
            .OnDelete(DeleteBehavior.Cascade); // If account is deleted, suggestion is irrelevant
    }
}
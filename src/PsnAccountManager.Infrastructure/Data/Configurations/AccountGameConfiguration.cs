using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class AccountGameConfiguration : IEntityTypeConfiguration<AccountGame>
{
    public void Configure(EntityTypeBuilder<AccountGame> builder)
    {
        builder.ToTable("AccountGames");

        // Composite Primary Key
        builder.HasKey(ag => new { ag.AccountId, ag.GameId });

        builder.Property(ag => ag.AccountId).HasColumnName("account_id");
        builder.Property(ag => ag.GameId).HasColumnName("game_id");
        builder.Property(ag => ag.IsPrimary).HasColumnName("is_primary").IsRequired();

        // --- Relationships ---
        builder.HasOne(ag => ag.Account)
            .WithMany(a => a.AccountGames)
            .HasForeignKey(ag => ag.AccountId);

        builder.HasOne(ag => ag.Game)
            .WithMany(g => g.AccountGames)
            .HasForeignKey(ag => ag.GameId);
    }
}
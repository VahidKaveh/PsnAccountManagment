using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class RequestGameConfiguration : IEntityTypeConfiguration<RequestGame>
{
    public void Configure(EntityTypeBuilder<RequestGame> builder)
    {
        builder.ToTable("RequestGames");

        builder.HasKey(rg => new { rg.RequestId, rg.GameId });

        // --- Column Mappings ---
        builder.Property(rg => rg.RequestId).HasColumnName("request_id");
        builder.Property(rg => rg.GameId).HasColumnName("game_id");

        // --- Relationships ---
        builder.HasOne(rg => rg.Request)
            .WithMany(r => r.RequestGames)
            .HasForeignKey(rg => rg.RequestId);

        builder.HasOne(rg => rg.Game)
            .WithMany(g => g.RequestGames)
            .HasForeignKey(rg => rg.GameId);
    }
}
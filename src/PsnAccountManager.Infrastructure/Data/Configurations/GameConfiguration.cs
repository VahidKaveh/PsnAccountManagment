using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Games");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasColumnName("game_id");

        builder.Property(g => g.SonyCode)
            .HasColumnName("sony_code")
            .HasMaxLength(100);

        builder.Property(g => g.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(g => g.Title)
            .IsUnique();

        builder.Property(g => g.Region)
            .HasColumnName("region")
            .HasMaxLength(50);

        builder.Property(g => g.PosterUrl)
            .HasColumnName("poster_url")
            .HasMaxLength(500);


        builder.Property(g => g.Description)
            .HasColumnName("description")
            .IsRequired(false)
            .HasMaxLength(1000);
    }
}
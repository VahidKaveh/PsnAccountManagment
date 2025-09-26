using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class GuideConfiguration : IEntityTypeConfiguration<Guide>
{
    public void Configure(EntityTypeBuilder<Guide> builder)
    {
        builder.ToTable("Guides");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("guide_id");

        builder.Property(g => g.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
        builder.Property(g => g.Content).HasColumnName("content").IsRequired();
        builder.Property(g => g.MediaUrl).HasColumnName("media_url").HasMaxLength(500);
        builder.Property(g => g.IsActive).HasColumnName("is_active").IsRequired();
    }
}
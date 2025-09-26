using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("user_id");

        builder.Property(u => u.TelegramId)
            .HasColumnName("telegram_id")
            .IsRequired();

        builder.HasIndex(u => u.TelegramId).IsUnique();

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(100);

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion(
                s => s.ToString(),
                s => (UserStatus)Enum.Parse(typeof(UserStatus), s));

        builder.Property(u => u.LastActiveAt).HasColumnName("last_active_at");
    }
}
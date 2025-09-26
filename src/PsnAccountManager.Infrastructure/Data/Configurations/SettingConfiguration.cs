using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");

        builder.HasKey(s => s.Key);

        // --- Column Mappings ---
        builder.Property(s => s.Key)
            .HasColumnName("key")
            .HasMaxLength(100); // It's good practice to set a max length for string keys

        builder.Property(s => s.Value)
            .HasColumnName("value")
            .IsRequired(); // A setting must have a value
    }
}
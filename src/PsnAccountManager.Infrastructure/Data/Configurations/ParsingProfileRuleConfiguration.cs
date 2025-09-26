using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class ParsingProfileRuleConfiguration : IEntityTypeConfiguration<ParsingProfileRule>
{
    public void Configure(EntityTypeBuilder<ParsingProfileRule> builder)
    {
        builder.ToTable("ParsingProfileRules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ParsingProfileId).IsRequired();

        builder.Property(r => r.FieldType)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(r => r.RegexPattern)
            .IsRequired()
            .HasMaxLength(500);

        // A profile cannot have two rules for the same field type
        builder.HasIndex(r => new { r.ParsingProfileId, r.FieldType }).IsUnique();
    }
}
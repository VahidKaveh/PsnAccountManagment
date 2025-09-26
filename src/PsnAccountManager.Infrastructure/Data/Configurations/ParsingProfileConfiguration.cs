using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

public class ParsingProfileConfiguration : IEntityTypeConfiguration<ParsingProfile>
{
    public void Configure(EntityTypeBuilder<ParsingProfile> builder)
    {
        builder.ToTable("ParsingProfiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);

        // A profile has many rules. If a profile is deleted, its rules are also deleted.
        builder.HasMany(p => p.Rules)
            .WithOne(r => r.Profile)
            .HasForeignKey(r => r.ParsingProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
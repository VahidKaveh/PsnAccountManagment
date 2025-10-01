using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for LearningData
/// Configures machine learning training data storage
/// </summary>
public class LearningDataConfiguration : IEntityTypeConfiguration<LearningData>
{
    public void Configure(EntityTypeBuilder<LearningData> builder)
    {
        builder.ToTable("LearningData");
        builder.HasKey(ld => ld.Id);

        builder.Property(ld => ld.ChannelId)
            .IsRequired();

 
        builder.Property(ld => ld.RawMessageId);


        builder.Property(ld => ld.EntityType)
            .IsRequired()
            .HasMaxLength(50);


        builder.Property(ld => ld.EntityValue)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ld => ld.OriginalText)
            .IsRequired(false)
            .HasColumnType("TEXT");


        builder.Property(ld => ld.TextContext)
            .IsRequired(false)
            .HasMaxLength(500);

 
        builder.Property(ld => ld.ConfidenceLevel)
            .IsRequired();

        builder.Property(ld => ld.IsManualCorrection)
            .IsRequired()
            .HasDefaultValue(false);


        builder.Property(ld => ld.IsUsedInTraining)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ld => ld.CreatedBy)
            .IsRequired(false)
            .HasMaxLength(100);


        builder.Property(ld => ld.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");


        builder.HasIndex(ld => ld.ChannelId);

        builder.HasIndex(ld => ld.EntityType);


        builder.HasIndex(ld => ld.IsManualCorrection);


        builder.HasIndex(ld => ld.IsUsedInTraining);

        builder.HasIndex(ld => new { ld.ChannelId, ld.EntityType });

        builder.HasOne(ld => ld.Channel)
            .WithMany(c => c.LearningData)
            .HasForeignKey(ld => ld.ChannelId)
            .OnDelete(DeleteBehavior.Cascade); // If channel deleted, delete learning data

        builder.HasOne(ld => ld.RawMessage)
            .WithMany() // No navigation property on RawMessage side
            .HasForeignKey(ld => ld.RawMessageId)
            .OnDelete(DeleteBehavior.Restrict); // If message deleted, keep learning data
    }
}

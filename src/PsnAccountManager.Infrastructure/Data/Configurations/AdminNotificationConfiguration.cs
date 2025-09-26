using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;


public class AdminNotificationConfiguration : IEntityTypeConfiguration<AdminNotification>
{
    public void Configure(EntityTypeBuilder<AdminNotification> builder)
    {
        builder.ToTable("AdminNotifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Message).IsRequired();
        builder.Property(n => n.IsRead).HasDefaultValue(false);
    }
}
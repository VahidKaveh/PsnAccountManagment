using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsnAccountManager.Domain.Entities;

public class AccountHistoryConfiguration : IEntityTypeConfiguration<AccountHistory>
{
    public void Configure(EntityTypeBuilder<AccountHistory> builder)
    {
        builder.ToTable("AccountHistories");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.FieldName).IsRequired().HasMaxLength(100);
        builder.Property(h => h.ChangedBy).IsRequired().HasMaxLength(100);

        // If an account is deleted, its history is also deleted.
        builder.HasOne(h => h.Account)
            .WithMany(a => a.History)
            .HasForeignKey(h => h.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;
using System.Reflection;

namespace PsnAccountManager.Infrastructure.Data;

public class PsnAccountManagerDbContext(DbContextOptions<PsnAccountManagerDbContext> options) : DbContext(options)
{
    // DbSets for all entities
    public DbSet<User> Users { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<AccountGame> AccountGames { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<RequestGame> RequestGames { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Dispute> Disputes { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<Guide> Guides { get; set; }
    public DbSet<PurchaseSuggestion> PurchaseSuggestions { get; set; }
    public DbSet<ParsingProfile> ParsingProfiles { get; set; }
    public DbSet<ParsingProfileRule> ParsingProfileRules { get; set; }
    public DbSet<RawMessage> RawMessages { get; set; }
    public DbSet<AccountHistory> AccountHistories { get; set; }
    public DbSet<AdminNotification> AdminNotifications { get; set; }
    private static AccountCapacity ToAccountCapacity(string value)
    {
        return value.ToLower() switch
        {
            "offline" or "offlineonly" or "z1" => AccountCapacity.Z1,
            "primary" or "z2" => AccountCapacity.Z2,
            "secondary" or "z3" => AccountCapacity.Z3,
            _ => AccountCapacity.Unknown
        };
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This line automatically applies all configurations from IEntityTypeConfiguration
        // classes in the current assembly. This is the cleanest approach.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<Setting>().HasData(
            new Setting { Key = "Matcher.MinMatchedGames", Value = "1" },
            new Setting { Key = "Matcher.MaxSuggestions", Value = "5" },
            new Setting { Key = "Matcher.SuggestionSortOrder", Value = "ByMatchedGames" },
            new Setting { Key = "ScraperWorker.ScrapeIntervalMinutes", Value = "15" }
        );

        var capacityConverter = new ValueConverter<AccountCapacity, string>(
            // Convert Enum to string for storing in DB
            v => v.ToString(),
            // Convert string from DB to Enum
            v => ToAccountCapacity(v)
        );

        modelBuilder
            .Entity<Account>()
            .Property(e => e.Capacity)
            .HasConversion(capacityConverter);
        // **END: Add this ValueConverter**
    }
}
  
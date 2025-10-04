using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Entities;

/// <summary>
/// Represents a source channel for scraping accounts
/// </summary>
public class Channel : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;

    public ChannelStatus Status { get; set; }
    public DateTime? LastScrapedAt { get; set; }
    public string? LastScrapedMessageId { get; set; }
    public int? ParsingProfileId { get; set; }

    // Scraper Configuration
    public TelegramFetchMode TelegramFetchMode { get; set; } = TelegramFetchMode.SinceLastMessage;
    public int? FetchValue { get; set; }
    public int DelayAfterScrapeMs { get; set; } = 1000;

    // Navigation Properties
    public virtual ParsingProfile? ParsingProfile { get; set; }
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    public virtual ICollection<Purchase> Sales { get; set; } = new List<Purchase>();
    public virtual ICollection<RawMessage> RawMessages { get; set; } = new List<RawMessage>();
}
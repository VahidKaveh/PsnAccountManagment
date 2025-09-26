using PsnAccountManager.Shared.Enums;
using System.Collections.Generic;

namespace PsnAccountManager.Domain.Entities;

public class Channel : BaseEntity<int>
{
    public string Name { get; set; }
    public ChannelStatus Status { get; set; }
    public DateTime? LastScrapedAt { get; set; }
    public int? LastScrapedMessageId { get; set; }

    public int? ParsingProfileId { get; set; }
    public virtual ParsingProfile? ParsingProfile { get; set; }

    /// <summary>
    /// The strategy to use for fetching messages from this channel.
    /// </summary>
    public FetchMode FetchMode { get; set; } = FetchMode.SinceLastMessage; // Default value

    /// <summary>
    /// The value associated with the FetchMode.
    /// - For LastXMessages, this is the number of messages.
    /// - For SinceXHoursAgo, this is the number of hours.
    /// </summary>
    public int? FetchValue { get; set; } // Nullable, as it's not needed for the default mode

    /// <summary>
    /// An optional delay in milliseconds to wait after scraping this channel
    /// before moving to the next one.
    /// </summary>
    public int DelayAfterScrapeMs { get; set; } = 1000; // Default to 1 second
    public virtual ICollection<RawMessage> RawMessages { get; set; } = new List<RawMessage>();

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    public virtual ICollection<Purchase> Sales { get; set; } = new List<Purchase>();
}
using PsnAccountManager.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Domain.Entities;

/// <summary>
/// Represents a PlayStation account with games and pricing information
/// </summary>
public class Account : BaseEntity<int>
{
    // Basic Information
    public int ChannelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;

    // Pricing
    public decimal? PricePs4 { get; set; }
    public decimal? PricePs5 { get; set; }
    public string? Region { get; set; }

    // Account Details
    public bool HasOriginalMail { get; set; }
    public int? GuaranteeMinutes { get; set; }
    public string? SellerInfo { get; set; }

    // Status
    public AccountCapacity Capacity { get; set; }
    public StockStatus StockStatus { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime LastScrapedAt { get; set; }
    public string? RecentChanges { get; set; }
    public string? AdditionalInfo { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }


    // Processing Information
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingResult { get; set; }
    public int? RawMessageId { get; set; }

    // Navigation Properties
    public virtual Channel Channel { get; set; } = null!;
    public virtual RawMessage? RawMessage { get; set; }
    public virtual ICollection<AccountGame> AccountGames { get; set; } = new List<AccountGame>();
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public virtual ICollection<AccountHistory> History { get; set; } = new List<AccountHistory>();
}
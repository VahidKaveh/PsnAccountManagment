using PsnAccountManager.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Domain.Entities;

public class Account : BaseEntity<int>
{
    public int ChannelId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ExternalId { get; set; }

    public decimal? PricePs4 { get; set; }
    public decimal? PricePs5 { get; set; }

    public string? Region { get; set; }
    public bool HasOriginalMail { get; set; }
    public int? GuaranteeMinutes { get; set; }
    public string? SellerInfo { get; set; }
    [StringLength(100)]
    public string? RecentChanges { get; set; }

    [StringLength(500)]
    public string? AdditionalInfo { get; set; }


   
    public AccountCapacity Capacity { get; set; }

    public StockStatus StockStatus { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime LastScrapedAt { get; set; }

    public virtual Channel Channel { get; set; }
    public virtual ICollection<AccountGame> AccountGames { get; set; } = new List<AccountGame>();
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public virtual ICollection<AccountHistory> History { get; set; } = new List<AccountHistory>();
}

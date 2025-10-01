using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Entities;

/// <summary>
/// Represents a purchase transaction
/// </summary>
public class Purchase : BaseEntity<int>
{
    public int BuyerUserId { get; set; }
    public int SellerChannelId { get; set; }
    public int AccountId { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual User Buyer { get; set; } = null!;
    public virtual Channel SellerChannel { get; set; } = null!;
    public virtual Account Account { get; set; } = null!;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();
}
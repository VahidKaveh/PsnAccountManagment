using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Entities;
public class Payment : BaseEntity<int>
{
    public int PurchaseId { get; set; }
    public decimal Amount { get; set; }
    public string Provider { get; set; } // e.g., "ZarinPal", "IDPay"
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual Purchase Purchase { get; set; }
}
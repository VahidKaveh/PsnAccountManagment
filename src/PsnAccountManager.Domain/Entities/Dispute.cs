using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Entities;

public class Dispute : BaseEntity<int>
{
    public int PurchaseId { get; set; }
    public int RaisedByUserId { get; set; }
    public string Reason { get; set; }
    public DisputeStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Purchase Purchase { get; set; }
    public virtual User RaisedBy { get; set; }
}
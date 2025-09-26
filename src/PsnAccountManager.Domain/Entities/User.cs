using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Entities;

public class User : BaseEntity<int>
{
    public long TelegramId { get; set; }
    public string Username { get; set; }
    public UserStatus Status { get; set; }
    public DateTime LastActiveAt { get; set; }
    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
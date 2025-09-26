namespace PsnAccountManager.Domain.Entities;

public class AdminNotification : BaseEntity<int>
{
    public string Message { get; set; }
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
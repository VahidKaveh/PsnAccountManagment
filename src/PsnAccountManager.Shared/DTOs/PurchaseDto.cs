namespace PsnAccountManager.Shared.DTOs;

public class PurchaseDto
{
    public int Id { get; set; }

    // Buyer Info
    public int BuyerUserId { get; set; }
    public string BuyerUsername { get; set; } // اطلاعات بیشتر برای نمایش

    // Account Info
    public int AccountId { get; set; }
    public string AccountTitle { get; set; } // اطلاعات بیشتر برای نمایش

    // Purchase Details
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
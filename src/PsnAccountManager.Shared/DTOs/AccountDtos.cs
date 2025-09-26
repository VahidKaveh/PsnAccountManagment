namespace PsnAccountManager.Shared.DTOs;

public class AccountDto
{
    public int Id { get; set; }
    public int ChannelId { get; set; }
    public string Title { get; set; }
    public decimal? PricePs4 { get; set; } // Updated
    public decimal? PricePs5 { get; set; } // Updated
    public string? Region { get; set; }
    public string Capacity { get; set; } // Updated from enum
    public string StockStatus { get; set; }
    public bool IsDeleted { get; set; }
    public List<GameDto> Games { get; set; } = new();
}
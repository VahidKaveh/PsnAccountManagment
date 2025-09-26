namespace PsnAccountManager.Domain.Entities;

public class PurchaseSuggestion : BaseEntity<int>
{
    public int UserId { get; set; }
    public int? RequestId { get; set; } // Can be nullable if suggestion is generic
    public int AccountId { get; set; }
    public string MatchedGames { get; set; } // e.g., "123,456"
    public double Rank { get; set; } // A score from 0.0 to 1.0

    public virtual User User { get; set; }
    public virtual Request Request { get; set; }
    public virtual Account Account { get; set; }
}
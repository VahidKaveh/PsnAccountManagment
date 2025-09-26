using PsnAccountManager.Shared.Enums;
namespace PsnAccountManager.Domain.Entities;

public class Request : BaseEntity<int>
{
    public int UserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public RequestStatus Status { get; set; }

    public virtual User User { get; set; }
    public virtual ICollection<RequestGame> RequestGames { get; set; } = new List<RequestGame>();
    public virtual ICollection<PurchaseSuggestion> Suggestions { get; set; } = new List<PurchaseSuggestion>();
}
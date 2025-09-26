namespace PsnAccountManager.Domain.Entities;

public class Game : BaseEntity<int>
{
    public string? SonyCode { get; set; } // Unique identifier from Sony
    public string Title { get; set; }
    public string? Region { get; set; }
    public string? PosterUrl { get; set; }
    public virtual ICollection<AccountGame> AccountGames { get; set; } = new List<AccountGame>();
    public virtual ICollection<RequestGame> RequestGames { get; set; } = new List<RequestGame>();
}
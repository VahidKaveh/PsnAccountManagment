namespace PsnAccountManager.Domain.Entities;

/// <summary>
/// Represents a PlayStation game
/// </summary>
public class Game : BaseEntity<int>
{
    public string? SonyCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Region { get; set; }
    public string? PosterUrl { get; set; }

    // Navigation Properties
    public virtual ICollection<AccountGame> AccountGames { get; set; } = new List<AccountGame>();
    public virtual ICollection<RequestGame> RequestGames { get; set; } = new List<RequestGame>();
}
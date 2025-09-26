namespace PsnAccountManager.Domain.Entities;

public class Wishlist : BaseEntity<int>
{
    public int UserId { get; set; }
    public int GameId { get; set; }

    public virtual User User { get; set; }
    public virtual Game Game { get; set; }
}
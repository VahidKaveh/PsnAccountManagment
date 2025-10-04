namespace PsnAccountManager.Domain.Entities;

public class AccountGame
{
    public int AccountId { get; set; }
    public int GameId { get; set; }
    public bool IsPrimary { get; set; }
    public virtual Account Account { get; set; }
    public virtual Game Game { get; set; }
}
namespace PsnAccountManager.Domain.Entities;

public class RequestGame
{
    public int RequestId { get; set; }
    public int GameId { get; set; }
    public virtual Request Request { get; set; }
    public virtual Game Game { get; set; }
}
namespace PsnAccountManager.Domain.Entities;

public class ParsingProfile : BaseEntity<int>
{
    public string Name { get; set; }
    public virtual List<ParsingProfileRule> Rules { get; set; } = new List<ParsingProfileRule>();
    public virtual ICollection<Channel> Channels { get; set; } = new List<Channel>();
}
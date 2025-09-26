namespace PsnAccountManager.Domain.Entities;
public class Guide : BaseEntity<int>
{
    public string Title { get; set; }
    public string Content { get; set; }
    public string? MediaUrl { get; set; }
    public bool IsActive { get; set; }
}
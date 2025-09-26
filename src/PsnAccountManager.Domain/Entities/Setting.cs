namespace PsnAccountManager.Domain.Entities;

// This entity uses a string key, so we don't use the generic BaseEntity<int>
public class Setting
{
    public string Key { get; set; }
    public string Value { get; set; }
}
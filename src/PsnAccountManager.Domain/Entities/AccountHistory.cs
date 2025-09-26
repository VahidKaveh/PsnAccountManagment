using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsnAccountManager.Domain.Entities;

public class AccountHistory : BaseEntity<int>
{
    public int AccountId { get; set; }
    public string FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string ChangedBy { get; set; }

    [ForeignKey("AccountId")]
    public virtual Account Account { get; set; }
}
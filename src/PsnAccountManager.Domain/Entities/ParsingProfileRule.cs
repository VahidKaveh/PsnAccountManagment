using System.ComponentModel.DataAnnotations.Schema;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Entities;

public class ParsingProfileRule : BaseEntity<int>
{
    public int ParsingProfileId { get; set; }
    public ParsedFieldType FieldType { get; set; }
    public string RegexPattern { get; set; }

    [ForeignKey(nameof(ParsingProfileId))] public virtual ParsingProfile Profile { get; set; }
}
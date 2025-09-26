using PsnAccountManager.Shared.Enums;
using System.ComponentModel.DataAnnotations.Schema; 
using System.Text.Json.Serialization;

namespace PsnAccountManager.Domain.Entities;

public class ParsingProfileRule : BaseEntity<int>
{
    public int ParsingProfileId { get; set; }
    public ParsedFieldType FieldType { get; set; }
    public string RegexPattern { get; set; }

    [ForeignKey("ParsingProfileId")] 
    [JsonIgnore] 
    public virtual ParsingProfile Profile { get; set; }
}
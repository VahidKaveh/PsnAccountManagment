using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PsnAccountManager.Shared.DTOs;

public class ParsedAccountDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public bool IsSold { get; set; }

    [JsonIgnore]
    public string? RawGamesBlock { get; set; }

    public List<string> ExtractedGames { get; set; } = new();

    [Obsolete("Use ExtractedGames instead")]
    [JsonIgnore]
    public List<string> Games
    {
        get => ExtractedGames;
        set => ExtractedGames = value;
    }

    public string? Region { get; set; }
    public decimal? PricePs4 { get; set; }
    public decimal? PricePs5 { get; set; }
    public bool HasOriginalMail { get; set; }
    public int? GuaranteeMinutes { get; set; }
    public string? SellerInfo { get; set; }
    public string? CapacityInfo { get; set; }
    public string? AdditionalInfo { get; set; }
}
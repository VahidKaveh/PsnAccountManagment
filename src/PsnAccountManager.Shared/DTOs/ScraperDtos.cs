using System.Collections.Generic;
using System.Text.Json.Serialization;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Shared.DTOs;

public class ParsedAccountDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public bool IsSold { get; set; }

    [JsonIgnore] public string? RawGamesBlock { get; set; }
    public List<string> ExtractedGames { get; set; } = new();

    public List<GamePreviewDto> Games { get; set; } = new();


    public string? Region { get; set; }
    public decimal? PricePs4 { get; set; }
    public decimal? PricePs5 { get; set; }
    public bool HasOriginalMail { get; set; }
    public int? GuaranteeMinutes { get; set; }
    public string? SellerInfo { get; set; }
    public string? CapacityInfo { get; set; }
    public AccountCapacity Capacity { get; set; }
    public string? AdditionalInfo { get; set; }
    public int RawMessageId { get; set; }
}

public class ParsingRuleDto
{
    public int Id { get; set; }

    /// <summary>
    /// The type of field this rule extracts (e.g., "Price", "Region", "Games").
    /// </summary>
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// The regular expression used to find and extract the data.
    /// </summary>
    public string RegexPattern { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the rule is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The priority of the rule, used to determine the order of execution.
    /// </summary>
    public int Priority { get; set; }
}
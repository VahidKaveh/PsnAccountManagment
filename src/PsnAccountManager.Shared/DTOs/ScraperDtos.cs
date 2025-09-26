using PsnAccountManager.Shared.ViewModels;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// Represents the structured data extracted from a single raw message.
/// This is the output of the IMessageParser.
/// </summary>
public class ParsedAccountDto
{
    // --- Core Info ---
    public string ExternalId { get; set; }
    public string Title { get; set; } // A generated summary title
    public string FullDescription { get; set; } // The complete, original text of the message
    public bool IsSold { get; set; } // Flag indicating if the account is sold

    // --- Extracted Attributes ---
    [JsonIgnore] // We don't need to send this to the client
    public string? RawGamesBlock { get; set; }
    public List<ParsedGameViewModel> Games { get; set; } = new();
    public string? Region { get; set; }
    public decimal? PricePs4 { get; set; }
    public decimal? PricePs5 { get; set; }
    public bool HasOriginalMail { get; set; }
    public int? GuaranteeMinutes { get; set; }
    public string? SellerInfo { get; set; }
    public string? CapacityInfo { get; set; } // Raw text like "Z1 — Only Offline"
    public string AdditionalInfo { get; set; }
}
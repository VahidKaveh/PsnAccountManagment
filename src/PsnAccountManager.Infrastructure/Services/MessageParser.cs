using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PsnAccountManager.Infrastructure.Services;

/// <summary>
/// A simplified, rule-based message parser using pure Regex.
/// This service is responsible for all text parsing logic.
/// </summary>
public class MessageParser : IMessageParser
{
    private readonly ILogger<MessageParser> _logger;

    // Regex patterns for parsing specific account details
    private static readonly Regex PricePs4Pattern =
        new(@"(?:PS4|Price PS4)[:\s]*([\d,]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PricePs5Pattern =
        new(@"(?:PS5|Price PS5)[:\s]*([\d,]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RegionPattern =
        new(@"(?:Region|ریجن)[:\s]*([A-Z]{2,3})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SoldStatusPattern =
        new(@"(SOLD|فروخته شده)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GamesBlockStartPattern = new(@"(?:Games|بازی‌ها|لیست بازی‌ها)[:\s]*\n",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex GamesBlockEndPattern = new(@"(?:─|Price|قیمت|Region|ریجن)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex CapacityPattern = new(
        @"(?:ظرفیت|Capacity|Zarfiat)[:\s]*\b(Z1|Z2|Z3|1|2|3)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public MessageParser(ILogger<MessageParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parses the message text to extract account details using regex.
    /// </summary>
    public Task<ParsedAccountDto?> ParseAccountMessageAsync(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            _logger.LogWarning("Cannot parse an empty or whitespace message.");
            return Task.FromResult<ParsedAccountDto?>(null);
        }

        try
        {
            var cleanText = RemoveEmojisAndDecorations(messageText);

            var dto = new ParsedAccountDto
            {
                PricePs4 = ParseDecimal(PricePs4Pattern, cleanText),
                PricePs5 = ParseDecimal(PricePs5Pattern, cleanText),
                Region = ParseString(RegionPattern, cleanText),
                IsSold = SoldStatusPattern.IsMatch(cleanText),
                Capacity = ParseCapacity(cleanText),
                ExtractedGames = ExtractGameTitles(cleanText),
                FullDescription = messageText
            };

            // Generate a default title if none is found
            dto.Title = $"Account with {dto.ExtractedGames.Count} games";

            return Task.FromResult<ParsedAccountDto?>(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during message parsing.");
            return Task.FromResult<ParsedAccountDto?>(null);
        }
    }

    private List<string> ExtractGameTitles(string text)
    {
        var gamesText = text;

        // Isolate the games block
        var startMatch = GamesBlockStartPattern.Match(text);
        if (startMatch.Success) gamesText = text.Substring(startMatch.Index + startMatch.Length);

        var endMatch = GamesBlockEndPattern.Match(gamesText);
        if (endMatch.Success) gamesText = gamesText.Substring(0, endMatch.Index);

        // Extract titles from the block
        return gamesText
            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => CleanGameTitle(line))
            .Where(cleanedLine => IsLikelyGameTitle(cleanedLine))
            .Distinct()
            .ToList();
    }

    private string CleanGameTitle(string title)
    {
        // Remove bullet points, numbers, and trim
        return Regex.Replace(title, @"^[•\-*\d\.]+\s*", "").Trim();
    }

    private bool IsLikelyGameTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 3 || text.Length > 100)
            return false;

        // Avoid lines that are just prices or system names
        var excludePatterns = new[] { @"^\d+$", @"^(PS4|PS5)$" };
        return !excludePatterns.Any(p => Regex.IsMatch(text, p, RegexOptions.IgnoreCase));
    }

    private decimal? ParseDecimal(Regex pattern, string text)
    {
        var match = pattern.Match(text);
        if (!match.Success) return null;

        var cleanedValue = Regex.Replace(match.Groups[1].Value, @"[^\d]", "");
        if (decimal.TryParse(cleanedValue, out var result)) return result;
        return null;
    }

    private string? ParseString(Regex pattern, string text)
    {
        var match = pattern.Match(text);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private string RemoveEmojisAndDecorations(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        // This regex removes a wide range of emojis and symbols
        return Regex.Replace(text, @"\p{Cs}|\p{So}|[^\u0000-\u007F]+", string.Empty).Trim();
    }

    #region Obsolete Methods - Kept for interface compliance if needed, but should be removed

    public Task<List<string>> ExtractGameTitlesAsync(string messageText)
    {
        return Task.FromResult(ExtractGameTitles(messageText));
    }

    public Task<decimal?> ExtractPriceAsync(string messageText, string platform = "PS4")
    {
        var pattern = platform.Equals("PS5", StringComparison.OrdinalIgnoreCase) ? PricePs5Pattern : PricePs4Pattern;
        return Task.FromResult(ParseDecimal(pattern, messageText));
    }

    public Task<string?> ExtractRegionAsync(string messageText)
    {
        return Task.FromResult(ParseString(RegionPattern, messageText));
    }

    public Task<bool> IsValidAccountMessageAsync(string messageText)
    {
        // A basic check: does it contain game or price info?
        return Task.FromResult(PricePs4Pattern.IsMatch(messageText) || PricePs5Pattern.IsMatch(messageText) ||
                               GamesBlockStartPattern.IsMatch(messageText));
    }

    public Task<MessageValidationResult> ValidateMessageAsync(string messageText)
    {
        var isValid = IsValidAccountMessageAsync(messageText).Result;
        return Task.FromResult(new MessageValidationResult
        {
            IsValid = isValid,
            ConfidenceScore = isValid ? 0.9 : 0.1
        });
    }

    public Task<bool> UpdateParsingRulesAsync(List<ParsingRuleDto> rules)
    {
        // This is now handled internally by Regex patterns, so this method is obsolete.
        _logger.LogWarning("UpdateParsingRulesAsync is obsolete and has no effect.");
        return Task.FromResult(true);
    }

    public Task<List<ParsingRuleDto>> GetParsingRulesAsync()
    {
        _logger.LogWarning("GetParsingRulesAsync is obsolete. Parsing rules are now hard-coded Regex patterns.");
        return Task.FromResult(new List<ParsingRuleDto>());
    }
    private AccountCapacity ParseCapacity(string text)
    {
        var match = CapacityPattern.Match(text);
        if (match.Success)
        {
            return match.Groups[1].Value.ToLower() switch
            {
                "z1" or "1" => AccountCapacity.Z1,
                "z2" or "2" => AccountCapacity.Z2,
                "z3" or "3" => AccountCapacity.Z3,
                _ => AccountCapacity.Unknown
            };
        }
        return AccountCapacity.Unknown; // Default value if not found
    }

    #endregion
}
using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PsnAccountManager.Infrastructure.Services;

/// <summary>
/// A dynamic, rule-based message parser.
/// It uses a profile of regex patterns to extract data, making it highly configurable.
/// </summary>
public class MessageParser : IMessageParser // Renaming the class to MessageParser to match DI registration
{
    private readonly ILogger<MessageParser> _logger;

    public MessageParser(ILogger<MessageParser> logger)
    {
        _logger = logger;
    }

    public ParsedAccountDto? Parse(string messageText, string externalId, ICollection<ParsingProfileRule> rules)
    {
        string cleanText = RemoveEmojisAndDecorations(messageText);

        var dto = new ParsedAccountDto
        {
            ExternalId = externalId,
            FullDescription = messageText
        };

        foreach (var rule in rules)
        {
            try
            {
                var match = Regex.Match(cleanText, rule.RegexPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
                if (!match.Success) continue;

                // The first captured group is usually the desired value
                string extractedValue = match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : match.Value.Trim();

                ApplyRule(dto, rule.FieldType, extractedValue);
            }
            catch (RegexMatchTimeoutException ex)
            {
                _logger.LogWarning(ex, "Regex timeout for rule {FieldType} with pattern {Pattern}", rule.FieldType, rule.RegexPattern);
            }
        }

        return dto;
    }

    private void ApplyRule(ParsedAccountDto dto, ParsedFieldType fieldType, string value)
    {
        // A helper to parse decimals safely, removing currency symbols and commas
        decimal? ParseDecimal(string val)
        {
            if (decimal.TryParse(Regex.Replace(val, @"[^\d.]", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return null;
        }

        switch (fieldType)
        {
            case ParsedFieldType.PricePs4:
                dto.PricePs4 = ParseDecimal(value);
                break;
            case ParsedFieldType.PricePs5:
                dto.PricePs5 = ParseDecimal(value);
                break;
            case ParsedFieldType.Region:
                dto.Region = value;
                break;
            case ParsedFieldType.OriginalMail:
                dto.HasOriginalMail = !string.IsNullOrEmpty(value); // Presence of a match means true
                break;
            case ParsedFieldType.Guarantee:
                if (int.TryParse(value, out var minutes)) dto.GuaranteeMinutes = minutes;
                break;
            case ParsedFieldType.SellerInfo:
                dto.SellerInfo = value;
                break;
            case ParsedFieldType.SoldStatus:
                dto.IsSold = !string.IsNullOrEmpty(value); // Presence of a match means true
                break;
            case ParsedFieldType.Capacity:
                dto.CapacityInfo = value;
                break;
            case ParsedFieldType.AdditionalInfo:
                dto.AdditionalInfo = value;
                break;
                // case ParsedFieldType.GamesBlock:
                // Split the extracted block of text into individual game titles
                // dto.RawGamesBlock = value;
                //break;
        }
    }
    private string RemoveEmojisAndDecorations(string text)
    {
        // This regex matches most emojis and some decorative symbols
        return Regex.Replace(text, @"\p{Cs}|\p{So}|[🔺✍️🖤➡️📍💳🌑🫧💯❤️‍🔥🔠✅❌]", "").Trim();
    }
}
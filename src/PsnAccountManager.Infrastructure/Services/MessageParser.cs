using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PsnAccountManager.Infrastructure.Services;

/// <summary>
/// Enhanced rule-based message parser with machine learning data extraction
/// Parses messages using regex rules and creates LearningData for training
/// </summary>
public class MessageParser : IMessageParser
{
    private readonly ILogger<MessageParser> _logger;
    private readonly ILearningDataRepository _learningDataRepository;

    public MessageParser(
        ILogger<MessageParser> logger,
        ILearningDataRepository learningDataRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _learningDataRepository = learningDataRepository ?? throw new ArgumentNullException(nameof(learningDataRepository));
    }

    /// <summary>
    /// Parses a raw message using profile rules and creates learning data
    /// </summary>
    public async Task<ParsedAccountDto?> ParseAsync(
        string messageText,
        string externalId,
        int channelId,
        int? rawMessageId,
        ICollection<ParsingProfileRule> rules)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            _logger.LogWarning("Cannot parse empty message");
            return null;
        }

        string cleanText = RemoveEmojisAndDecorations(messageText);

        var dto = new ParsedAccountDto
        {
            ExternalId = externalId,
            FullDescription = messageText
        };

        // Track successful extractions for learning
        var learningDataList = new List<LearningData>();

        foreach (var rule in rules)
        {
            try
            {
                var match = Regex.Match(
                    cleanText,
                    rule.RegexPattern,
                    RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline,
                    TimeSpan.FromSeconds(2) // Timeout protection
                );

                if (!match.Success) continue;

                // The first captured group is usually the desired value
                string extractedValue = match.Groups.Count > 1
                    ? match.Groups[1].Value.Trim()
                    : match.Value.Trim();

                if (string.IsNullOrWhiteSpace(extractedValue))
                    continue;

                // Apply the rule to DTO
                ApplyRule(dto, rule.FieldType, extractedValue);

                // Create learning data for this extraction
                var learningData = CreateLearningData(
                    channelId,
                    rawMessageId,
                    rule.FieldType,
                    extractedValue,
                    messageText,
                    match
                );

                if (learningData != null)
                {
                    learningDataList.Add(learningData);
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                _logger.LogWarning(ex,
                    "Regex timeout for rule {FieldType} with pattern {Pattern}",
                    rule.FieldType, rule.RegexPattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error applying rule {FieldType} with pattern {Pattern}",
                    rule.FieldType, rule.RegexPattern);
            }
        }

        // Parse games block separately (if present)
        await ExtractGamesAsync(dto, cleanText, messageText, channelId, rawMessageId: (int)rawMessageId, learningDataList);

        // Save all learning data in one transaction
        if (learningDataList.Any())
        {
            try
            {
                foreach (var learningData in learningDataList)
                {
                    await _learningDataRepository.AddAsync(learningData);
                }
                await _learningDataRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "Created {Count} learning data entries for message {MessageId}",
                    learningDataList.Count, rawMessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save learning data");
            }
        }

        return dto;
    }

    /// <summary>
    /// Synchronous parse method (for backward compatibility)
    /// </summary>
    public ParsedAccountDto? Parse(
        string messageText,
        string externalId,
        ICollection<ParsingProfileRule> rules)
    {
        // Call async version without learning data
        return ParseAsync(messageText, externalId, 0, null, rules).GetAwaiter().GetResult();
    }

    private void ApplyRule(ParsedAccountDto dto, ParsedFieldType fieldType, string value)
    {
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
                dto.HasOriginalMail = !string.IsNullOrEmpty(value);
                break;

            case ParsedFieldType.Guarantee:
                if (int.TryParse(value, out var minutes))
                    dto.GuaranteeMinutes = minutes;
                break;

            case ParsedFieldType.SellerInfo:
                dto.SellerInfo = value;
                break;

            case ParsedFieldType.SoldStatus:
                dto.IsSold = !string.IsNullOrEmpty(value);
                break;

            case ParsedFieldType.Capacity:
                dto.CapacityInfo = value;
                break;

            case ParsedFieldType.AdditionalInfo:
                dto.AdditionalInfo = value;
                break;
        }
    }

    /// <summary>
    /// Extracts game titles from message text
    /// Supports multiple formats: bullet points, numbered lists, comma-separated
    /// </summary>
    private async Task ExtractGamesAsync(
        ParsedAccountDto dto,
        string cleanText,
        string originalText,
        int channelId,
        int rawMessageId,
        List<LearningData> learningDataList)
    {
        var gamesList = new List<string>();

        // Pattern 1: Bullet points or dashes
        var bulletPattern = @"[•\-*]\s*([^\n•\-*]+)";
        var bulletMatches = Regex.Matches(cleanText, bulletPattern);

        foreach (Match match in bulletMatches)
        {
            var game = match.Groups[1].Value.Trim();
            if (IsLikelyGameTitle(game))
            {
                gamesList.Add(game);

                // Create learning data
                learningDataList.Add(new LearningData
                {
                    ChannelId = channelId,
                    RawMessageId = rawMessageId,
                    EntityType = "Game",
                    EntityValue = game,
                    OriginalText = originalText,
                    TextContext = GetContext(originalText, game, 50),
                    ConfidenceLevel = 85, // Bullet points are usually high confidence
                    IsManualCorrection = false,
                    IsUsedInTraining = false,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Pattern 2: Numbered lists (1. Game, 2. Game)
        var numberedPattern = @"\d+\.\s*([^\n\d]+)";
        var numberedMatches = Regex.Matches(cleanText, numberedPattern);

        foreach (Match match in numberedMatches)
        {
            var game = match.Groups[1].Value.Trim();
            if (IsLikelyGameTitle(game) && !gamesList.Contains(game))
            {
                gamesList.Add(game);

                learningDataList.Add(new LearningData
                {
                    ChannelId = channelId,
                    RawMessageId = rawMessageId,
                    EntityType = "Game",
                    EntityValue = game,
                    OriginalText = originalText,
                    TextContext = GetContext(originalText, game, 50),
                    ConfidenceLevel = 85,
                    IsManualCorrection = false,
                    IsUsedInTraining = false,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Pattern 3: Comma or slash separated
        if (gamesList.Count == 0)
        {
            var separatorPattern = @"([^,/\n]+)";
            var separatorMatches = Regex.Matches(cleanText, separatorPattern);

            foreach (Match match in separatorMatches)
            {
                var game = match.Value.Trim();
                if (IsLikelyGameTitle(game) && !gamesList.Contains(game) && gamesList.Count < 20)
                {
                    gamesList.Add(game);

                    learningDataList.Add(new LearningData
                    {
                        ChannelId = channelId,
                        RawMessageId = rawMessageId,
                        EntityType = "Game",
                        EntityValue = game,
                        OriginalText = originalText,
                        TextContext = GetContext(originalText, game, 50),
                        ConfidenceLevel = 60, // Lower confidence for separator-based
                        IsManualCorrection = false,
                        IsUsedInTraining = false,
                        CreatedBy = "system",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        dto.ExtractedGames = gamesList;
    }

    /// <summary>
    /// Checks if a string is likely a game title
    /// </summary>
    private bool IsLikelyGameTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (text.Length < 3 || text.Length > 100) return false;

        // Exclude common non-game phrases
        var excludePatterns = new[]
        {
            "price", "قیمت", "region", "منطقه", "sold", "فروخته",
            "ps4", "ps5", "account", "اکانت", "mail", "ایمیل"
        };

        return !excludePatterns.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a LearningData object from a successful extraction
    /// </summary>
    private LearningData? CreateLearningData(
        int channelId,
        int? rawMessageId,
        ParsedFieldType fieldType,
        string extractedValue,
        string originalText,
        Match match)
    {
        if (rawMessageId == null) return null;

        // Calculate confidence level based on match quality
        int confidence = CalculateConfidence(match, fieldType);

        // Get surrounding context
        string context = GetContext(originalText, extractedValue, 50);

        return new LearningData
        {
            ChannelId = channelId,
            RawMessageId = (int)rawMessageId,
            EntityType = fieldType.ToString(),
            EntityValue = extractedValue,
            OriginalText = originalText,
            TextContext = context,
            ConfidenceLevel = confidence,
            IsManualCorrection = false,
            IsUsedInTraining = false,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Calculates confidence level based on match quality
    /// </summary>
    private int CalculateConfidence(Match match, ParsedFieldType fieldType)
    {
        int baseConfidence = 70;

        // Boost confidence for certain field types
        if (fieldType == ParsedFieldType.PricePs4 || fieldType == ParsedFieldType.PricePs5)
        {
            // Price patterns are usually reliable
            baseConfidence = 90;
        }
        else if (fieldType == ParsedFieldType.Region)
        {
            baseConfidence = 85;
        }

        // Boost if match is exact and clean
        if (match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
        {
            baseConfidence += 5;
        }

        return Math.Min(baseConfidence, 100);
    }

    /// <summary>
    /// Gets surrounding context for an extracted value
    /// </summary>
    private string GetContext(string fullText, string extractedValue, int contextLength)
    {
        int index = fullText.IndexOf(extractedValue, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return extractedValue;

        int start = Math.Max(0, index - contextLength);
        int end = Math.Min(fullText.Length, index + extractedValue.Length + contextLength);

        return fullText.Substring(start, end - start).Trim();
    }

    /// <summary>
    /// Safely parses decimal values, removing currency symbols and commas
    /// </summary>
    private decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Remove currency symbols, commas, and non-numeric characters except dots
        string cleaned = Regex.Replace(value, @"[^\d.]", "");

        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Removes emojis and decorative characters from text
    /// </summary>
    private string RemoveEmojisAndDecorations(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove most emojis and decorative symbols
        text = Regex.Replace(text, @"\p{Cs}|\p{So}|[🔺✍️🖤➡️📍💳🌑🫧💯❤️🔥🔠✅❌]", "");

        return text.Trim();
    }
}

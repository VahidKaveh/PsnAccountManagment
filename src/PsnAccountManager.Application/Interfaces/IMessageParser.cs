using System.Collections.Generic;
using System.Threading.Tasks;
using PsnAccountManager.Shared.DTOs;

namespace PsnAccountManager.Application.Interfaces;

/// <summary>
/// Defines the contract for a simplified, regex-based message parser.
/// </summary>
public interface IMessageParser
{
    /// <summary>
    /// Parses the given message text and extracts account details.
    /// </summary>
    /// <param name="messageText">The raw text from the message.</param>
    /// <returns>A DTO containing the extracted account information.</returns>
    Task<ParsedAccountDto?> ParseAccountMessageAsync(string messageText);

    /// <summary>
    /// Extracts a list of game titles from the message text.
    /// </summary>
    /// <param name="messageText">The message text to parse.</param>
    /// <returns>A list of extracted game titles.</returns>
    Task<List<string>> ExtractGameTitlesAsync(string messageText);

    /// <summary>
    /// Extracts a price for a specific platform (e.g., "PS4" or "PS5").
    /// </summary>
    /// <param name="messageText">The message text to parse.</param>
    /// <param name="platform">The platform to look for.</param>
    /// <returns>The extracted price, or null if not found.</returns>
    Task<decimal?> ExtractPriceAsync(string messageText, string platform = "PS4");

    /// <summary>
    /// Extracts the region information from the message text.
    /// </summary>
    /// <param name="messageText">The message text to parse.</param>
    /// <returns>The extracted region string, or null if not found.</returns>
    Task<string?> ExtractRegionAsync(string messageText);

    /// <summary>
    /// Performs a quick validation to check if a message is likely a valid account message.
    /// </summary>
    /// <param name="messageText">The message text to validate.</param>
    /// <returns>True if the message seems valid, otherwise false.</returns>
    Task<bool> IsValidAccountMessageAsync(string messageText);

    /// <summary>
    /// Provides a detailed validation result for a message.
    /// </summary>
    /// <param name="messageText">The message text to validate.</param>
    /// <returns>A result object containing validation status and issues.</returns>
    Task<MessageValidationResult> ValidateMessageAsync(string messageText);

    /// <summary>
    /// Updates the parsing rules used by the parser. (Note: Obsolete in pure Regex implementation).
    /// </summary>
    /// <param name="rules">A list of parsing rules.</param>
    /// <returns>True if the operation was successful.</returns>
    Task<bool> UpdateParsingRulesAsync(List<ParsingRuleDto> rules);

    /// <summary>
    /// Retrieves the current parsing rules. (Note: Obsolete in pure Regex implementation).
    /// </summary>
    /// <returns>A list of current parsing rules.</returns>
    Task<List<ParsingRuleDto>> GetParsingRulesAsync();
}
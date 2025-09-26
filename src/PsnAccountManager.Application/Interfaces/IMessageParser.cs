using PsnAccountManager.Domain.Entities; // For ParsingProfileRule
using PsnAccountManager.Shared.DTOs;
using System.Collections.Generic;

namespace PsnAccountManager.Application.Interfaces;

/// <summary>
/// Defines a dynamic parser that can extract structured data from raw text
/// based on a given set of parsing rules.
/// </summary>
public interface IMessageParser
{
    /// <summary>
    /// Parses a raw message text using a collection of regex-based rules.
    /// </summary>
    /// <param name="messageText">The raw text of the message to parse.</param>
    /// <param name="externalId">The unique external identifier for the message.</param>
    /// <param name="rules">A collection of rules that define how to extract each field.</param>
    /// <returns>A DTO containing the extracted data, or null if parsing is not possible.</returns>
    ParsedAccountDto? Parse(string messageText, string externalId, ICollection<ParsingProfileRule> rules);
}
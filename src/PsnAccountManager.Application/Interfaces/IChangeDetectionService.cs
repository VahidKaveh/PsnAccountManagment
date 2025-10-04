using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.DTOs;

namespace PsnAccountManager.Application.Interfaces
{
    public interface IChangeDetectionService
    {
        /// <summary>
        /// Generates a SHA256 hash of the normalized message content
        /// </summary>
        string GenerateContentHash(string messageText);

        /// <summary>
        /// Checks if content has changed compared to the last version
        /// </summary>
        Task<bool> HasContentChangedAsync(int channelId, string externalId, string newHash);

        /// <summary>
        /// Gets the most recent RawMessage for the given external ID
        /// </summary>
        Task<RawMessage?> GetPreviousMessageAsync(int channelId, string externalId);

        /// <summary>
        /// Compares two parsed account data objects and returns detailed changes
        /// </summary>
        ChangeDetails DetectChanges(ParsedAccountDto? oldData, ParsedAccountDto? newData);

        /// <summary>
        /// Normalizes message text for consistent hashing
        /// </summary>
        string NormalizeMessageText(string messageText);
    }
}
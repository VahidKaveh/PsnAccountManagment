using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using System.Security.Cryptography;
using System.Text;

namespace PsnAccountManager.Application.Services;

/// <summary>
/// Service for detecting content changes in raw messages.
/// Implements hashing, comparison, and detailed change detection logic.
/// </summary>
public class ChangeDetectionService : IChangeDetectionService
{
    private readonly IRawMessageRepository _rawMessageRepo;
    private readonly ILogger<ChangeDetectionService> _logger;

    public ChangeDetectionService(
        IRawMessageRepository rawMessageRepo,
        ILogger<ChangeDetectionService> logger)
    {
        _rawMessageRepo = rawMessageRepo;
        _logger = logger;
    }

    /// <summary>
    /// Generates a SHA256 hash of the normalized message content for change detection.
    /// </summary>
    /// <param name="messageText">The raw message text to hash</param>
    /// <returns>SHA256 hash string</returns>
    public string GenerateContentHash(string messageText)
    {
        // Normalize the message text before hashing to ignore insignificant differences
        var normalized = NormalizeMessageText(messageText);
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Normalizes message text by removing extra whitespace, normalizing line endings,
    /// and trimming to ensure consistent hashing.
    /// </summary>
    /// <param name="messageText">The message text to normalize</param>
    /// <returns>Normalized text string</returns>
    public string NormalizeMessageText(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            return string.Empty;

        // Normalize line endings to \n
        var normalized = messageText.Replace("\r\n", "\n").Replace("\r", "\n");
        
        // Remove excessive whitespace while preserving single spaces
        var lines = normalized.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));
        
        return string.Join("\n", lines).Trim();
    }

    /// <summary>
    /// Checks if the content has changed by comparing the new hash with the previous message's hash.
    /// Also sets Status to PendingChange and IsChange to true if a change is detected.
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <param name="externalId">The external message ID</param>
    /// <param name="newHash">The new content hash to compare</param>
    /// <returns>True if content has changed, false otherwise</returns>
    public async Task<bool> HasContentChangedAsync(int channelId, string externalId, string newHash)
    {
        try
        {
            // Get the most recent message for this external ID in this channel
            var previousMessage = await GetPreviousMessageAsync(channelId, externalId);
            
            if (previousMessage == null)
            {
                // No previous message found - this is a new message, not a change
                _logger.LogDebug("No previous message found for channel {ChannelId}, external ID {ExternalId}",
                    channelId, externalId);
                return false;
            }

            // Compare hashes
            var hasChanged = !string.Equals(previousMessage.ContentHash, newHash, StringComparison.OrdinalIgnoreCase);
            
            if (hasChanged)
            {
                _logger.LogInformation("Content change detected for channel {ChannelId}, external ID {ExternalId}. " +
                    "Old hash: {OldHash}, New hash: {NewHash}",
                    channelId, externalId, previousMessage.ContentHash, newHash);
            }
            
            return hasChanged;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking content change for channel {ChannelId}, external ID {ExternalId}",
                channelId, externalId);
            return false;
        }
    }

    /// <summary>
    /// Gets the most recent (previous) RawMessage for a given channel and external message ID.
    /// This is used to find the message to compare against when detecting changes.
    /// </summary>
    /// <param name="channelId">The channel ID</param>
    /// <param name="externalId">The external message ID</param>
    /// <returns>The previous RawMessage or null if none exists</returns>
    public async Task<RawMessage?> GetPreviousMessageAsync(int channelId, string externalId)
    {
        try
        {
            // Get the most recent message with this external ID in this channel,
            // ordered by creation date descending (most recent first)
            var previousMessage = await _rawMessageRepo.GetAll()
                .Where(m => m.ChannelId == channelId && m.ExternalMessageId.ToString() == externalId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            return previousMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving previous message for channel {ChannelId}, external ID {ExternalId}",
                channelId, externalId);
            return null;
        }
    }

    /// <summary>
    /// Detects and catalogs all changes between old and new parsed account data.
    /// Generates a detailed ChangeDetails object with field-by-field comparison.
    /// </summary>
    /// <param name="oldData">The previous parsed account data</param>
    /// <param name="newData">The new parsed account data</param>
    /// <returns>ChangeDetails object containing all detected changes</returns>
    public ChangeDetails DetectChanges(ParsedAccountDto? oldData, ParsedAccountDto? newData)
    {
        var changes = new ChangeDetails();

        // Handle null cases
        if (oldData == null && newData == null)
        {
            return changes; // No changes
        }

        if (oldData == null)
        {
            // New data with no previous data - treat as all new
            changes.ChangeType = ChangeType.Created;
            if (newData != null)
            {
                changes.AddChange("Title", "-", newData.Title ?? "-");
                changes.AddChange("Description", "-", TruncateForDisplay(newData.Description, 200));
            }
            return changes;
        }

        if (newData == null)
        {
            // Old data exists but new is null - deleted
            changes.ChangeType = ChangeType.Deleted;
            changes.AddChange("Status", "Active", "Deleted");
            return changes;
        }

        // Compare each field and record changes
        
        // Title comparison
        if (!string.Equals(oldData.Title, newData.Title, StringComparison.Ordinal))
        {
            changes.AddChange("Title", oldData.Title ?? "-", newData.Title ?? "-");
        }

        // Description comparison (truncated for display)
        var oldDesc = TruncateForDisplay(oldData.Description, 200);
        var newDesc = TruncateForDisplay(newData.Description, 200);
        if (!string.Equals(oldDesc, newDesc, StringComparison.Ordinal))
        {
            changes.AddChange("Description", oldDesc, newDesc);
        }

        // Price PS4 comparison
        if (oldData.PricePs4 != newData.PricePs4)
        {
            changes.AddChange("PricePS4", FormatPrice(oldData.PricePs4), FormatPrice(newData.PricePs4));
        }

        // Price PS5 comparison
        if (oldData.PricePs5 != newData.PricePs5)
        {
            changes.AddChange("PricePS5", FormatPrice(oldData.PricePs5), FormatPrice(newData.PricePs5));
        }

        // Region comparison
        if (!string.Equals(oldData.Region, newData.Region, StringComparison.OrdinalIgnoreCase))
        {
            changes.AddChange("Region", oldData.Region ?? "-", newData.Region ?? "-");
        }

        // Seller info comparison
        if (!string.Equals(oldData.SellerInfo, newData.SellerInfo, StringComparison.Ordinal))
        {
            changes.AddChange("SellerInfo", 
                TruncateForDisplay(oldData.SellerInfo, 100), 
                TruncateForDisplay(newData.SellerInfo, 100));
        }

        // Additional info comparison
        if (!string.Equals(oldData.AdditionalInfo, newData.AdditionalInfo, StringComparison.Ordinal))
        {
            changes.AddChange("AdditionalInfo", 
                TruncateForDisplay(oldData.AdditionalInfo, 100), 
                TruncateForDisplay(newData.AdditionalInfo, 100));
        }

        // Games comparison (list)
        if (!AreStringListsEqual(oldData.ExtractedGames, newData.ExtractedGames))
        {
            var oldGames = string.Join(", ", (oldData.ExtractedGames ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)));
            var newGames = string.Join(", ", (newData.ExtractedGames ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)));
            changes.AddChange("ExtractedGames", TruncateForDisplay(oldGames, 200), TruncateForDisplay(newGames, 200));
        }

        // Determine the overall change type based on what changed
        changes.ChangeType = DetermineChangeType(changes);

        return changes;
    }

    // ==================== PRIVATE HELPER METHODS ====================

    /// <summary>
    /// Truncates a string to a maximum length for display purposes, adding ellipsis if truncated.
    /// </summary>
    private static string TruncateForDisplay(string? value, int max)
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length <= max) return v;
        return v.Substring(0, Math.Max(0, max - 1)) + "…";
    }

    /// <summary>
    /// Formats a decimal price for display, showing "-" for null values.
    /// </summary>
    private static string FormatPrice(decimal? price)
    {
        if (price == null) return "-";
        return string.Format("{0:N0}", price.Value);
    }

    /// <summary>
    /// Formats a guarantee time in minutes as hours or minutes display string.
    /// </summary>
    private static string FormatGuarantee(int? minutes)
    {
        if (minutes == null) return "-";
        var m = minutes.Value;
        if (m <= 0) return "0m";
        if (m % 60 == 0) return $"{m / 60}h";
        return $"{m}m";
    }

    /// <summary>
    /// Compares two string lists for equality, ignoring order and case.
    /// </summary>
    private static bool AreStringListsEqual(IEnumerable<string>? a, IEnumerable<string>? b)
    {
        var aa = (a ?? Enumerable.Empty<string>()).Select(s => s?.Trim() ?? string.Empty).Where(s => s.Length > 0).OrderBy(s => s);
        var bb = (b ?? Enumerable.Empty<string>()).Select(s => s?.Trim() ?? string.Empty).Where(s => s.Length > 0).OrderBy(s => s);
        return aa.SequenceEqual(bb, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines the overall change type based on which fields changed.
    /// Prioritizes certain change types (Deleted, PriceChanged, StatusChanged) over generic Modified.
    /// </summary>
    private static ChangeType DetermineChangeType(ChangeDetails details)
    {
        // Heuristic: prioritize Sold/Status/Price, else generic Modified.
        var keys = details.Changes.Select(c => c.Field).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (keys.Contains("SoldStatus")) return ChangeType.StatusChanged;
        if (keys.Contains("PricePS4") || keys.Contains("PricePS5")) return ChangeType.PriceChanged;
        if (keys.Count == 0) return ChangeType.NoChange;
        return ChangeType.Modified;
    }

    /// <summary>
    /// Builds a short summary string showing the old and new values.
    /// Used for generating human-readable change summaries.
    /// </summary>
    private static string BuildShortSummary(string? oldText, string? newText)
    {
        var oldNorm = (oldText ?? string.Empty).Replace('\n', ' ');
        var newNorm = (newText ?? string.Empty).Replace('\n', ' ');
        var oldShort = TruncateForDisplay(oldNorm, 80);
        var newShort = TruncateForDisplay(newNorm, 80);
        if (string.IsNullOrEmpty(oldShort)) return newShort;
        return $"{oldShort} => {newShort}";
    }
}

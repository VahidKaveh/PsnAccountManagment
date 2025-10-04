using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using System.Security.Cryptography;
using System.Text;

namespace PsnAccountManager.Application.Services
{
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

        public string GenerateContentHash(string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                return string.Empty;

            var normalized = NormalizeMessageText(messageText);

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(normalized);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        public async Task<bool> HasContentChangedAsync(int channelId, string externalId, string newHash)
        {
            try
            {
                var lastMessage = await _rawMessageRepo.GetByExternalIdAsync(channelId, externalId);

                if (lastMessage == null)
                    return true; // First message, treat as new

                return lastMessage.ContentHash != newHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking content changes for {ChannelId}:{ExternalId}",
                    channelId, externalId);
                return true; // Assume changed to be safe
            }
        }

        public async Task<RawMessage?> GetPreviousMessageAsync(int channelId, string externalId)
        {
            try
            {
                return await _rawMessageRepo.GetByExternalIdAsync(channelId, externalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous message for {ChannelId}:{ExternalId}",
                    channelId, externalId);
                return null;
            }
        }

        public ChangeDetails DetectChanges(ParsedAccountDto? oldData, ParsedAccountDto? newData)
        {
            var changes = new ChangeDetails();

            // Handle new account
            if (oldData == null && newData != null)
            {
                changes.ChangeType = ChangeType.New;
                return changes;
            }

            // Handle deleted account
            if (oldData != null && newData == null)
            {
                changes.ChangeType = ChangeType.Deleted;
                return changes;
            }

            // Handle no data
            if (oldData == null && newData == null)
            {
                changes.ChangeType = ChangeType.NoChange;
                return changes;
            }

            // Compare all fields for changes
            CompareAccountFields(oldData!, newData!, changes);

            // Determine primary change type
            if (changes.HasChanges)
            {
                changes.ChangeType = DetermineChangeType(changes);
            }

            return changes;
        }

        public string NormalizeMessageText(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
                return string.Empty;

            // **FIX: Don't convert to lowercase for hash generation**
            // Convert to lowercase will make different content produce same hash
            return messageText
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\t", " ")
                .Trim();
        }

        private void CompareAccountFields(ParsedAccountDto oldData, ParsedAccountDto newData, ChangeDetails changes)
        {
            // Compare ExternalId (shouldn't change, but just in case)
            if (!string.Equals(oldData.ExternalId?.Trim(), newData.ExternalId?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                changes.AddChange("ExternalId", oldData.ExternalId, newData.ExternalId);
            }

            // Compare Title
            if (!string.Equals(oldData.Title?.Trim(), newData.Title?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                changes.AddChange("Title", oldData.Title, newData.Title);
            }

            // Compare FullDescription
            if (!string.Equals(oldData.FullDescription?.Trim(), newData.FullDescription?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                changes.AddChange("Description",
                    TruncateForDisplay(oldData.FullDescription, 100),
                    TruncateForDisplay(newData.FullDescription, 100));
            }

            // Compare IsSold status
            if (oldData.IsSold != newData.IsSold)
            {
                changes.AddChange("SoldStatus",
                    oldData.IsSold ? "Sold" : "Available",
                    newData.IsSold ? "Sold" : "Available");
            }

            // Compare Region
            if (!string.Equals(oldData.Region?.Trim(), newData.Region?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                changes.AddChange("Region", oldData.Region, newData.Region);
            }

            // Compare Prices
            if (oldData.PricePs4 != newData.PricePs4)
            {
                changes.AddChange("PricePS4",
                    FormatPrice(oldData.PricePs4),
                    FormatPrice(newData.PricePs4));
            }

            if (oldData.PricePs5 != newData.PricePs5)
            {
                changes.AddChange("PricePS5",
                    FormatPrice(oldData.PricePs5),
                    FormatPrice(newData.PricePs5));
            }

            // Compare HasOriginalMail
            if (oldData.HasOriginalMail != newData.HasOriginalMail)
            {
                changes.AddChange("OriginalMail",
                    oldData.HasOriginalMail ? "Yes" : "No",
                    newData.HasOriginalMail ? "Yes" : "No");
            }

            // Compare GuaranteeMinutes
            if (oldData.GuaranteeMinutes != newData.GuaranteeMinutes)
            {
                changes.AddChange("Guarantee",
                    FormatGuarantee(oldData.GuaranteeMinutes),
                    FormatGuarantee(newData.GuaranteeMinutes));
            }

            // Compare SellerInfo
            if (!string.Equals(oldData.SellerInfo?.Trim(), newData.SellerInfo?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                changes.AddChange("SellerInfo", oldData.SellerInfo, newData.SellerInfo);
            }

            // Compare CapacityInfo
            if (!string.Equals(oldData.CapacityInfo?.Trim(), newData.CapacityInfo?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                changes.AddChange("CapacityInfo", oldData.CapacityInfo, newData.CapacityInfo);
            }

            // Compare Capacity Enum
            if (oldData.Capacity != newData.Capacity)
            {
                changes.AddChange("Capacity", oldData.Capacity.ToString(), newData.Capacity.ToString());
            }

            // Compare AdditionalInfo
            if (!string.Equals(oldData.AdditionalInfo?.Trim(), newData.AdditionalInfo?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                changes.AddChange("AdditionalInfo",
                    TruncateForDisplay(oldData.AdditionalInfo, 50),
                    TruncateForDisplay(newData.AdditionalInfo, 50));
            }

            // Compare ExtractedGames list
            if (!AreStringListsEqual(oldData.ExtractedGames, newData.ExtractedGames))
            {
                var oldGames = string.Join(", ", oldData.ExtractedGames ?? new List<string>());
                var newGames = string.Join(", ", newData.ExtractedGames ?? new List<string>());
                changes.AddChange("ExtractedGames",
                    TruncateForDisplay(oldGames, 200),
                    TruncateForDisplay(newGames, 200));
            }

            // Compare Games list (GamePreviewDto objects)
            if (!AreGamePreviewListsEqual(oldData.Games, newData.Games))
            {
                var oldGameTitles = string.Join(", ", oldData.Games?.Select(g => g.Title) ?? new List<string>());
                var newGameTitles = string.Join(", ", newData.Games?.Select(g => g.Title) ?? new List<string>());
                changes.AddChange("Games",
                    TruncateForDisplay(oldGameTitles, 200),
                    TruncateForDisplay(newGameTitles, 200));
            }
        }

        private ChangeType DetermineChangeType(ChangeDetails changes)
        {
            var fieldTypes = changes.Changes.Select(c => c.Field.ToLower()).ToHashSet();

            // Check for sold status change first (most important)
            if (fieldTypes.Contains("soldstatus"))
                return ChangeType.Deleted; // Treat sold as deleted

            // Check for price changes
            if (fieldTypes.Contains("priceps4") || fieldTypes.Contains("priceps5"))
                return ChangeType.PriceChanged;

            // Check for games changes
            if (fieldTypes.Contains("games") || fieldTypes.Contains("extractedgames"))
                return ChangeType.GamesChanged;

            // Check for region changes
            if (fieldTypes.Contains("region"))
                return ChangeType.RegionChanged;

            return ChangeType.Modified;
        }

        private string? FormatPrice(decimal? price)
        {
            return price?.ToString("C") ?? "N/A";
        }

        private string FormatGuarantee(int? minutes)
        {
            if (!minutes.HasValue) return "N/A";

            if (minutes < 60)
                return $"{minutes}m";

            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;

            return remainingMinutes == 0 ? $"{hours}h" : $"{hours}h {remainingMinutes}m";
        }

        private string TruncateForDisplay(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            return text.Length <= maxLength ? text : text[..maxLength] + "...";
        }

        private bool AreStringListsEqual(IEnumerable<string>? list1, IEnumerable<string>? list2)
        {
            var set1 = (list1 ?? Enumerable.Empty<string>())
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var set2 = (list2 ?? Enumerable.Empty<string>())
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return set1.SetEquals(set2);
        }

        private bool AreGamePreviewListsEqual(IEnumerable<GamePreviewDto>? list1, IEnumerable<GamePreviewDto>? list2)
        {
            var titles1 = (list1 ?? Enumerable.Empty<GamePreviewDto>())
                .Where(g => !string.IsNullOrWhiteSpace(g.Title))
                .Select(g => g.Title.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var titles2 = (list2 ?? Enumerable.Empty<GamePreviewDto>())
                .Where(g => !string.IsNullOrWhiteSpace(g.Title))
                .Select(g => g.Title.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return titles1.SetEquals(titles2);
        }
    }
}

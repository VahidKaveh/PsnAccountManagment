using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PsnAccountManager.Application.Services
{
    public class ChangeDetectionService : IChangeDetectionService
    {
        private readonly IRawMessageRepository _rawMessageRepo;
        private readonly ILogger<ChangeDetectionService> _logger;
        private readonly IAccountRepository _accountRepository;

        public ChangeDetectionService(
            IRawMessageRepository rawMessageRepo,
            ILogger<ChangeDetectionService> logger,
            IAccountRepository accountRepository)
        {
            _rawMessageRepo = rawMessageRepo;
            _logger = logger;
            _accountRepository = accountRepository;
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
                _logger.LogDebug("Checking content changes for Channel:{ChannelId}, ExternalId:{ExternalId}", 
                    channelId, externalId);
                
                // استفاده از متد موجود در repository
                var lastMessage = await _rawMessageRepo.GetByExternalIdAsync(channelId, externalId);

                if (lastMessage == null)
                {
                    _logger.LogInformation("No previous message found for {ChannelId}:{ExternalId}, treating as new", 
                        channelId, externalId);
                    return true; // First message, treat as new
                }

                var hasChanged = lastMessage.ContentHash != newHash;
                
                _logger.LogDebug("Content change check result: {HasChanged}. Old hash: {OldHash}, New hash: {NewHash}", 
                    hasChanged, lastMessage.ContentHash, newHash);
                
                return hasChanged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking content changes for {ChannelId}:{ExternalId}",
                    channelId, externalId);
                return true; // Assume changed to be safe
            }
        }

        // NEW METHOD: برای بررسی تغییر پیغام با message ID
        public async Task<bool> HasMessageChangedAsync(long messageId, string currentText, int channelId)
        {
            try
            {
                _logger.LogDebug($"Checking for changes in message {messageId}");

                var currentHash = GenerateContentHash(currentText);
                
                // استفاده از متد موجود - تبدیل messageId به string
                var existingMessage = await _rawMessageRepo.GetByExternalIdAsync(channelId, messageId.ToString());

                if (existingMessage == null)
                {
                    _logger.LogInformation($"New message detected: {messageId}");
                    return false; // نو است، تغییر محسوب نمی‌شود
                }

                // بررسی اینکه آیا پیغام از کانال حذف شده (پیغام خالی یا null)
                bool isMessageDeleted = string.IsNullOrWhiteSpace(currentText) || currentText.Trim().Length == 0;

                if (isMessageDeleted)
                {
                    _logger.LogInformation($"Message {messageId} appears to be deleted from channel");
                    
                    // اگر پیغام در Inbox باشد و پردازش نشده، آن را به حالت Deleted تغییر دهیم
                    if (existingMessage.ProcessedAt == null)
                    {
                        _logger.LogInformation($"Marking unprocessed message {messageId} as deleted instead of creating duplicate change");
                        
                        existingMessage.Status = RawMessageStatus.Deleted;
                        existingMessage.UpdatedAt = DateTime.UtcNow;
                        existingMessage.UpdatedBy = "System";
                        existingMessage.ErrorMessage = "Message deleted from channel before processing";
                        
                        _rawMessageRepo.Update(existingMessage);
                        await _rawMessageRepo.SaveChangesAsync();
                        
                        return false; // هیچ change جدیدی ایجاد نکن
                    }
                    
                    // اگر قبلاً پردازش شده، change ایجاد کن
                    return true;
                }

                bool hasChanged = existingMessage.ContentHash != currentHash;

                if (hasChanged)
                {
                    _logger.LogInformation($"Content change detected for message {messageId}");
                    _logger.LogDebug($"Old hash: {existingMessage.ContentHash}, New hash: {currentHash}");
                }

                return hasChanged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking message changes for message {messageId}");
                return false;
            }
        }

        // NEW METHOD: برای ایجاد رکورد تغییر - بدون dependency به ProcessingService
        public async Task CreateChangeRecordAsync(long messageId, string newContent, string oldContent, int channelId)
        {
            try
            {
                _logger.LogInformation($"Creating change record for message {messageId}");

                var existingMessage = await _rawMessageRepo.GetByExternalIdAsync(channelId, messageId.ToString());
                if (existingMessage == null)
                {
                    _logger.LogWarning($"Cannot create change record - original message {messageId} not found");
                    return;
                }

                // تشخیص نوع تغییر
                string changeType = DetectChangeType(oldContent, newContent);
                string changeDetails = GenerateChangeDetails(oldContent, newContent, changeType);

                var changeRecord = new RawMessage
                {
                    ChannelId = channelId,
                    ExternalMessageId = messageId,
                    MessageText = newContent,
                    ContentHash = GenerateContentHash(newContent),
                    Status = RawMessageStatus.Pending,
                    ReceivedAt = DateTime.UtcNow,
                    IsChange = true,
                    PreviousMessageId = existingMessage.Id,
                    ChangeDetails = changeDetails,
                    AccountId = existingMessage.AccountId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "ChangeDetectionService",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                await _rawMessageRepo.AddAsync(changeRecord);
                await _rawMessageRepo.SaveChangesAsync();

                _logger.LogInformation($"Change record created successfully for message {messageId}, Change type: {changeType}");

                // بجای ProcessingService، فقط log می‌کنیم که change ایجاد شده
                _logger.LogInformation($"Change record {changeRecord.Id} created and is ready for processing");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating change record for message {messageId}");
                throw;
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
                _logger.LogInformation("Detected new account: {ExternalId}", newData.ExternalId);
                return changes;
            }

            // Handle deleted account
            if (oldData != null && newData == null)
            {
                changes.ChangeType = ChangeType.Deleted;
                _logger.LogInformation("Detected deleted account: {ExternalId}", oldData.ExternalId);
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
                _logger.LogInformation("Detected changes in account {ExternalId}: {ChangeType}, Total changes: {Count}", 
                    newData!.ExternalId, changes.ChangeType, changes.Changes.Count);
            }
            else
            {
                changes.ChangeType = ChangeType.NoChange;
            }

            return changes;
        }

        public string NormalizeMessageText(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
                return string.Empty;

            // IMPROVED: More comprehensive normalization for better change detection
            return messageText
                .ToLowerInvariant() // Convert to lowercase for consistent comparison
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\t", " ")
                .Normalize() // Unicode normalization first
                .Trim()
                .Replace("  ", " "); // Replace double spaces with single AFTER trim
        }

        // NEW HELPER METHODS for string-based change detection
        private string DetectChangeType(string oldContent, string newContent)
        {
            if (string.IsNullOrWhiteSpace(newContent))
                return "DELETED";

            if (string.IsNullOrWhiteSpace(oldContent))
                return "CREATED";

            // بررسی تغییرات قیمت
            var oldPrice = ExtractPrice(oldContent);
            var newPrice = ExtractPrice(newContent);

            if (oldPrice != newPrice)
                return "PRICE_CHANGED";

            // بررسی تغییرات وضعیت
            var oldStatus = ExtractAccountStatus(oldContent);
            var newStatus = ExtractAccountStatus(newContent);

            if (oldStatus != newStatus)
                return "STATUS_CHANGED";

            return "CONTENT_MODIFIED";
        }

        private string GenerateChangeDetails(string oldContent, string newContent, string changeType)
        {
            var details = new List<string>();

            switch (changeType)
            {
                case "DELETED":
                    details.Add("Account deleted from channel");
                    break;

                case "PRICE_CHANGED":
                    var oldPrice = ExtractPrice(oldContent);
                    var newPrice = ExtractPrice(newContent);
                    details.Add($"Price changed from {oldPrice} to {newPrice}");
                    break;

                case "STATUS_CHANGED":
                    var oldStatus = ExtractAccountStatus(oldContent);
                    var newStatus = ExtractAccountStatus(newContent);
                    details.Add($"Status changed from '{oldStatus}' to '{newStatus}'");
                    break;

                default:
                    details.Add("Content modified");
                    break;
            }

            return string.Join("; ", details);
        }

        private string ExtractPrice(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "Unknown";

            var priceMatch = Regex.Match(content, @"(\d+(?:\.\d+)?)\s*(?:تومان|تومن|T|Toman)", RegexOptions.IgnoreCase);
            return priceMatch.Success ? priceMatch.Groups[1].Value + " تومان" : "نامشخص";
        }

        private string ExtractAccountStatus(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "Unknown";

            var statusPatterns = new[]
            {
                @"(?:وضعیت|Status):\s*([^\n\r]+)",
                @"(?:sold|فروخته شده|فروخته)",
                @"(?:available|در دسترس|موجود)",
                @"(?:reserved|رزرو شده|رزرو)"
            };

            foreach (var pattern in statusPatterns)
            {
                var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : match.Value;
                }
            }

            return "نامشخص";
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

            // Compare IsSold status - THIS IS CRITICAL FOR CHANGE DETECTION
            if (oldData.IsSold != newData.IsSold)
            {
                changes.AddChange("SoldStatus",
                    oldData.IsSold ? "Sold" : "Available",
                    newData.IsSold ? "Sold" : "Available");
                    
                _logger.LogInformation("IMPORTANT: Sold status changed for {ExternalId}: {OldStatus} -> {NewStatus}", 
                    newData.ExternalId, 
                    oldData.IsSold ? "Sold" : "Available", 
                    newData.IsSold ? "Sold" : "Available");
            }

            // Compare Region
            if (!string.Equals(oldData.Region?.Trim(), newData.Region?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                changes.AddChange("Region", oldData.Region, newData.Region);
            }

            // Compare Prices - SENSITIVE TO SMALL CHANGES
            if (Math.Abs((oldData.PricePs4 ?? 0) - (newData.PricePs4 ?? 0)) > 0.01m)
            {
                changes.AddChange("PricePS4",
                    FormatPrice(oldData.PricePs4),
                    FormatPrice(newData.PricePs4));
            }

            if (Math.Abs((oldData.PricePs5 ?? 0) - (newData.PricePs5 ?? 0)) > 0.01m)
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
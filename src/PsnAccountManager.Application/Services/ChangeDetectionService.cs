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
                _logger.LogDebug("Checking content changes for Channel:{ChannelId}, ExternalId:{ExternalId}", channelId, externalId);
                var lastMessage = await _rawMessageRepo.GetByExternalIdAsync(channelId, externalId);
                if (lastMessage == null)
                {
                    _logger.LogInformation("No previous message found for {ChannelId}:{ExternalId}, treating as new", channelId, externalId);
                    return true; // First message, treat as new
                }
                var hasChanged = lastMessage.ContentHash != newHash;
                _logger.LogDebug("Content change check result: {HasChanged}. Old hash: {OldHash}, New hash: {NewHash}", hasChanged, lastMessage.ContentHash, newHash);
                if (hasChanged)
                {
                    lastMessage.Status = RawMessageStatus.PendingChange;
                    lastMessage.IsChange = true;
                    lastMessage.ChangeDetails = GenerateChangeDetails(lastMessage.MessageText ?? string.Empty, lastMessage.MessageText ?? string.Empty, "CONTENT_MODIFIED");
                    lastMessage.UpdatedAt = DateTime.UtcNow;
                    lastMessage.UpdatedBy = "ChangeDetectionService";
                    _rawMessageRepo.Update(lastMessage);
                    await _rawMessageRepo.SaveChangesAsync();
                }
                return hasChanged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking content changes for {ChannelId}:{ExternalId}", channelId, externalId);
                return true; // Assume changed to be safe
            }
        }

        public async Task<bool> HasMessageChangedAsync(long messageId, string currentText, int channelId)
        {
            try
            {
                _logger.LogDebug($"Checking for changes in message {messageId}");
                var currentHash = GenerateContentHash(currentText);
                var existingMessage = await _rawMessageRepo.GetByExternalIdAsync(channelId, messageId.ToString());
                if (existingMessage == null)
                {
                    _logger.LogInformation($"New message detected: {messageId}");
                    return false; // new
                }
                bool isMessageDeleted = string.IsNullOrWhiteSpace(currentText) || currentText.Trim().Length == 0;
                if (isMessageDeleted)
                {
                    _logger.LogInformation($"Message {messageId} appears to be deleted from channel");
                    if (existingMessage.ProcessedAt == null)
                    {
                        _logger.LogInformation($"Marking unprocessed message {messageId} as deleted instead of creating duplicate change");
                        existingMessage.Status = RawMessageStatus.Deleted;
                        existingMessage.UpdatedAt = DateTime.UtcNow;
                        existingMessage.UpdatedBy = "System";
                        existingMessage.ErrorMessage = "Message deleted from channel before processing";
                        _rawMessageRepo.Update(existingMessage);
                        await _rawMessageRepo.SaveChangesAsync();
                        return false;
                    }
                    return true;
                }
                bool hasChanged = existingMessage.ContentHash != currentHash;
                if (hasChanged)
                {
                    _logger.LogInformation($"Content change detected for message {messageId}");
                    _logger.LogDebug($"Old hash: {existingMessage.ContentHash}, New hash: {currentHash}");
                    existingMessage.Status = RawMessageStatus.PendingChange;
                    existingMessage.IsChange = true;
                    existingMessage.PreviousMessageId = existingMessage.Id;
                    existingMessage.ChangeDetails = GenerateChangeDetails(existingMessage.MessageText ?? string.Empty, currentText ?? string.Empty, DetectChangeType(existingMessage.MessageText ?? string.Empty, currentText ?? string.Empty));
                    existingMessage.ContentHash = currentHash;
                    existingMessage.MessageText = currentText;
                    existingMessage.UpdatedAt = DateTime.UtcNow;
                    existingMessage.UpdatedBy = "ChangeDetectionService";
                    if (existingMessage.AccountId.HasValue)
                    {
                        var account = await _accountRepository.GetByIdAsync(existingMessage.AccountId.Value);
                        if (account != null)
                        {
                            var recent = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {existingMessage.ChangeDetails}";
                            try { account.RecentChanges = recent; } catch { }
                            try { account.RecentChangeDescription = recent; } catch { }
                            _accountRepository.Update(account);
                        }
                    }
                    _rawMessageRepo.Update(existingMessage);
                    await _rawMessageRepo.SaveChangesAsync();
                    await _accountRepository.SaveChangesAsync();
                }
                return hasChanged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking message changes for message {messageId}");
                return false;
            }
        }

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
                string changeType = DetectChangeType(oldContent, newContent);
                string changeDetails = GenerateChangeDetails(oldContent, newContent, changeType);
                existingMessage.Status = RawMessageStatus.PendingChange;
                existingMessage.IsChange = true;
                existingMessage.ChangeDetails = changeDetails;
                existingMessage.UpdatedAt = DateTime.UtcNow;
                existingMessage.UpdatedBy = "ChangeDetectionService";
                _rawMessageRepo.Update(existingMessage);
                var changeRecord = new RawMessage
                {
                    ChannelId = channelId,
                    ExternalMessageId = messageId,
                    MessageText = newContent,
                    ContentHash = GenerateContentHash(newContent),
                    Status = RawMessageStatus.PendingChange,
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
                if (existingMessage.AccountId.HasValue)
                {
                    var account = await _accountRepository.GetByIdAsync(existingMessage.AccountId.Value);
                    if (account != null)
                    {
                        var recent = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {changeDetails}";
                        try { account.RecentChanges = recent; } catch { }
                        try { account.RecentChangeDescription = recent; } catch { }
                        _accountRepository.Update(account);
                    }
                }
                await _rawMessageRepo.SaveChangesAsync();
                await _accountRepository.SaveChangesAsync();
                _logger.LogInformation($"Change record created successfully for message {messageId}, Change type: {changeType}");
                _logger.LogInformation($"Change record created and is ready for processing");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating change record for message {messageId}");
                throw;
            }
        }

        public async Task<RawMessage?> GetPreviousMessageAsync(int channelId, string externalId)
        {
            try { return await _rawMessageRepo.GetByExternalIdAsync(channelId, externalId); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous message for {ChannelId}:{ExternalId}", channelId, externalId);
                return null;
            }
        }

        public ChangeDetails DetectChanges(ParsedAccountDto? oldData, ParsedAccountDto? newData)
        {
            var changes = new ChangeDetails();
            if (oldData == null && newData != null)
            { changes.ChangeType = ChangeType.New; _logger.LogInformation("Detected new account: {ExternalId}", newData.ExternalId); return changes; }
            if (oldData != null && newData == null)
            { changes.ChangeType = ChangeType.Deleted; _logger.LogInformation("Detected deleted account: {ExternalId}", oldData.ExternalId); return changes; }
            if (oldData == null && newData == null)
            { changes.ChangeType = ChangeType.NoChange; return changes; }
            CompareAccountFields(oldData!, newData!, changes);
            if (changes.HasChanges)
            { changes.ChangeType = DetermineChangeType(changes); _logger.LogInformation("Detected changes in account {ExternalId}: {ChangeType}, Total changes: {Count}", newData!.ExternalId, changes.ChangeType, changes.Changes.Count); }
            else { changes.ChangeType = ChangeType.NoChange; }
            return changes;
        }

        public string NormalizeMessageText(string messageText)
        {
            if (string.IsNullOrEmpty(messageText)) return string.Empty;
            return messageText.ToLowerInvariant().Replace("\r\n", "\n").Replace("\r", "\n").Replace("\t", " ").Normalize().Trim().Replace("  ", " ");
        }

        private string DetectChangeType(string oldContent, string newContent)
        {
            if (string.IsNullOrWhiteSpace(newContent)) return "DELETED";
            if (string.IsNullOrWhiteSpace(oldContent)) return "CREATED";
            var oldPrice = ExtractPrice(oldContent);
            var newPrice = ExtractPrice(newContent);
            if (oldPrice != newPrice) return "PRICE_CHANGED";
            var oldStatus = ExtractAccountStatus(oldContent);
            var newStatus = ExtractAccountStatus(newContent);
            if (oldStatus != newStatus) return "STATUS_CHANGED";
            return "CONTENT_MODIFIED";
        }

        private string GenerateChangeDetails(string oldContent, string newContent, string changeType)
        {
            var details = new List<string>();
            switch (changeType)
            {
                case "DELETED": details.Add("Account deleted from channel"); break;
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
                default: details.Add("Content modified"); break;
            }
            return string.Join("; ", details);
        }

        private string ExtractPrice(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "Unknown";
            var priceMatch = Regex.Match(content, @"(\d+(?:\.\d+)?)\s*(?:تومان|تومن|T|Toman)", RegexOptions.IgnoreCase);
            return priceMatch.Success ? priceMatch.Groups[1].Value + " تومان" : "نامشخص";
        }

        private string ExtractAccountStatus(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "Unknown";
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
            if (!string.Equals(oldData.ExternalId?.Trim(), newData.ExternalId?.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.AddChange("ExternalId", oldData.ExternalId, newData.ExternalId);
            if (!string.Equals(oldData.Title?.Trim(), newData.Title?.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.AddChange("Title", oldData.Title, newData.Title);
            if (!string.Equals(oldData.FullDescription?.Trim(), newData.FullDescription?.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.AddChange("Description", TruncateForDisplay(oldData.FullDescription, 100), TruncateForDisplay(newData.FullDescription, 100));
            if (oldData.IsSold != newData.IsSold)
            {
                changes.AddChange("SoldStatus", oldData.IsSold ? "Sold" : "Available", newData.IsSold ? "Sold" : "Available");
                _logger.LogInformation("IMPORTANT: Sold status changed for {ExternalId}: {OldStatus} -> {NewStatus}", newData.ExternalId, oldData.IsSold ? "Sold" : "Available", newData.IsSold ? "Sold" : "Available");
            }
            if (!string.Equals(oldData.Region?.Trim(), newData.Region?.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.AddChange("Region", oldData.Region, newData.Region);
            if (Math.Abs((oldData.PricePs4 ?? 0) - (newData.PricePs4 ?? 0)) > 0.01m)
                changes.AddChange("PricePS4", FormatPrice(oldData.PricePs4), FormatPrice(newData.PricePs4));
            if (Math.Abs((oldData.PricePs5 ?? 0) - (newData.PricePs5 ?? 0)) > 0.01m)
                changes.AddChange("PricePS5", FormatPrice(oldData.PricePs5), FormatPrice(newData.PricePs5));
            if (oldData.HasOriginalMail != newData.HasOriginalMail)
                changes.AddChange("OriginalMail", oldData.HasOriginalMail ? "Yes" : "No", newData.HasOriginalMail ? "Yes" : "No");
            if (oldData.GuaranteeMinutes != newData.GuaranteeMinutes)
                changes.AddChange("Guarantee", FormatGuarantee(oldData.GuaranteeMinutes), FormatGuarantee(newData.GuaranteeMinutes));
            if (!string.Equals(oldData.SellerInfo?.Trim(), newData.SellerInfo?.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.AddChange("SellerInfo", oldData.SellerInfo, newData.SellerInfo);
            if (!string.Equals(oldData.CapacityInfo?.Trim(), newData.CapacityInfo?.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.AddChange("CapacityInfo", oldData.CapacityInfo, newData.CapacityInfo);
            if (oldData.Capacity != newData.Capacity)
                changes.AddChange("Capacity", oldData.Capacity.ToString(), newData.Capacity.ToString());
            if (!string.Equals(oldData.AdditionalInfo?.Trim(), newData.AdditionalInfo?.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.AddChange("AdditionalInfo", TruncateForDisplay(oldData.AdditionalInfo, 50), TruncateForDisplay(newData.AdditionalInfo, 50));
            if (!AreStringListsEqual(oldData.ExtractedGames, newData.ExtractedGames))
            {
                var oldGames = string.Join(", ", oldData.ExtractedGames ?? new List<string>());
                var newGames = string.Join(", ", newData.ExtractedGames ?? new List<string>());
                changes.AddChange("ExtractedGames", TruncateForDisplay(oldGames, 200), TruncateForDisplay(newGames, 200));
            }
            if

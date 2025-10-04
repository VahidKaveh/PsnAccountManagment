using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace PsnAccountManager.Application.Services;

/// <summary>
/// Coordinates the process of parsing messages, creating/updating accounts,
/// and managing related entities in a transactional manner.
/// Enhanced with change detection and admin notifications.
/// </summary>
public class ProcessingService : IProcessingService
{
    private readonly IRawMessageRepository _rawMessageRepo;
    private readonly IMessageParser _messageParser;
    private readonly IAccountRepository _accountRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<ProcessingService> _logger;

    // ==================== SERVICES FOR CHANGE DETECTION ====================
    private readonly IChangeDetectionService _changeDetectionService;
    private readonly IAdminNotificationRepository _notificationRepository;
    private readonly IChannelRepository _channelRepository;
    private readonly IConfiguration _configuration;

    public ProcessingService(
        IRawMessageRepository rawMessageRepo,
        IMessageParser messageParser,
        IAccountRepository accountRepository,
        IGameRepository gameRepository,
        ILogger<ProcessingService> logger,
        IChangeDetectionService changeDetectionService,
        IAdminNotificationRepository notificationRepository,
        IChannelRepository channelRepository,
        IConfiguration configuration)
    {
        _rawMessageRepo = rawMessageRepo;
        _messageParser = messageParser;
        _accountRepository = accountRepository;
        _gameRepository = gameRepository;
        _logger = logger;
        _changeDetectionService = changeDetectionService;
        _notificationRepository = notificationRepository;
        _channelRepository = channelRepository;
        _configuration = configuration;
    }

    // ==================== DIRECT MESSAGE PROCESSING ====================
    /// <summary>
    /// Processes a single message by ID with enhanced error handling and change detection
    /// </summary>
    public async Task ProcessMessageAsync(int messageId)
    {
        try
        {
            var message = await _rawMessageRepo.GetByIdAsync(messageId);
            if (message == null)
            {
                _logger.LogWarning($"Message with ID {messageId} not found");
                return;
            }

            // اگر پیغام قبلاً پردازش شده
            if (message.ProcessedAt.HasValue)
            {
                _logger.LogInformation($"Message {messageId} already processed at {message.ProcessedAt}");
                return;
            }

            _logger.LogInformation($"Processing message {messageId}, IsChange: {message.IsChange}, Status: {message.Status}");

            // Update processing status
            message.Status = RawMessageStatus.Processing;
            message.UpdatedAt = DateTime.UtcNow;
            message.UpdatedBy = "ProcessingService";
            await _rawMessageRepo.UpdateAsync(message);
            await _rawMessageRepo.SaveChangesAsync();

            try
            {
                if (message.Status == RawMessageStatus.Deleted)
                {
                    await ProcessDeletedMessageAsync(message);
                }
                else if (message.IsChange)
                {
                    await ProcessChangeMessageAsync(message);
                }
                else
                {
                    await ProcessNewMessageAsync(message);
                }

                // Mark as successfully processed using repository method
                await _rawMessageRepo.MarkAsProcessedAsync(messageId, message.AccountId);

                _logger.LogInformation($"Message {messageId} processed successfully");
            }
            catch (Exception ex)
            {
                // Mark as failed using repository method
                await _rawMessageRepo.MarkAsFailedAsync(messageId, ex.Message);
                _logger.LogError(ex, $"Error processing message {messageId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Critical error processing message {messageId}");
            throw;
        }
    }

    /// <summary>
    /// Process messages that were deleted from channel before processing
    /// </summary>
    private async Task ProcessDeletedMessageAsync(RawMessage message)
    {
        _logger.LogInformation($"Processing deleted message {message.Id}");

        if (message.AccountId.HasValue)
        {
            var account = await _accountRepository.GetByIdAsync(message.AccountId.Value);
            if (account != null)
            {
                // Mark account as deleted/unavailable
                account.StockStatus = StockStatus.OutOfStock;
                account.UpdatedAt = DateTime.UtcNow;

                await _accountRepository.UpdateAsync(account);
                await _accountRepository.SaveChangesAsync();

                // Create admin notification
                await CreateAdminNotificationAsync(
                    AdminNotificationType.AccountDeleted,
                    "Account Deleted Before Processing",
                    $"Account '{account.Title}' was deleted from channel before processing",
                    NotificationPriority.High,
                    message.Id,
                    "RawMessage",
                    new { AccountId = account.Id, AccountTitle = account.Title }
                );

                _logger.LogInformation($"Account {account.Id} marked as out of stock due to message deletion");
            }
        }

        // Create admin notification for unprocessed deletion
        await CreateAdminNotificationAsync(
            AdminNotificationType.MessageDeleted,
            "Unprocessed Message Deleted",
            "A message was deleted from channel before admin review",
            NotificationPriority.Normal,
            message.Id,
            "RawMessage",
            new { ChannelId = message.ChannelId, ExternalMessageId = message.ExternalMessageId }
        );
    }

    /// <summary>
    /// Process change messages (updates to existing content)
    /// </summary>
    private async Task ProcessChangeMessageAsync(RawMessage message)
    {
        _logger.LogInformation($"Processing change message {message.Id}");

        if (!message.AccountId.HasValue)
        {
            _logger.LogWarning($"Change message {message.Id} has no associated account");
            return;
        }

        var account = await _accountRepository.GetByIdAsync(message.AccountId.Value);
        if (account == null)
        {
            _logger.LogWarning($"Account {message.AccountId} not found for change message {message.Id}");
            return;
        }

        // بررسی نوع تغییر و اعمال آن
        var changeDetails = message.ChangeDetails ?? "";
        var changeType = AdminNotificationType.AccountUpdated;
        
        if (changeDetails.Contains("DELETED"))
        {
            account.StockStatus = StockStatus.OutOfStock;
            account.UpdatedAt = DateTime.UtcNow;
            changeType = AdminNotificationType.AccountDeleted;
        }
        else if (changeDetails.Contains("PRICE_CHANGED"))
        {
            // استخراج قیمت جدید و به‌روزرسانی
            var newPriceMatch = Regex.Match(
                message.MessageText, 
                @"(\d+(?:\.\d+)?)\s*(?:تومان|تومن|T)", 
                RegexOptions.IgnoreCase
            );
            
            if (newPriceMatch.Success && decimal.TryParse(newPriceMatch.Groups.Value, out decimal newPrice))
            {
                // Determine if it's PS4 or PS5 price based on message content
                if (message.MessageText.ToLower().Contains("ps5"))
                {
                    account.PricePs5 = newPrice;
                }
                else
                {
                    account.PricePs4 = newPrice;
                }
                account.UpdatedAt = DateTime.UtcNow;
            }
            changeType = AdminNotificationType.PriceChanged;
        }
        else if (changeDetails.Contains("STATUS_CHANGED"))
        {
            // به‌روزرسانی وضعیت اکانت
            account.UpdatedAt = DateTime.UtcNow;
            changeType = AdminNotificationType.StatusChanged;
        }

        // Update description with new message content
        account.Description = message.MessageText;
        account.LastScrapedAt = DateTime.UtcNow;

        await _accountRepository.UpdateAsync(account);

        // ایجاد notification برای admin
        await CreateAdminNotificationAsync(
            changeType,
            $"Account '{account.Title}' Updated",
            $"Account has been updated with changes: {changeDetails}",
            NotificationPriority.Normal,
            account.Id,
            "Account",
            new { 
                ChangeDetails = changeDetails,
                MessageId = message.Id,
                ChannelId = message.ChannelId
            }
        );

        // Simple change tracking log (instead of using ChangeTrackerService)
        _logger.LogInformation("Change tracked for account {AccountId}: {ChangeDetails} at {Timestamp}", 
            account.Id, changeDetails, DateTime.UtcNow);

        _logger.LogInformation($"Account {account.Id} updated successfully with changes: {changeDetails}");
    }

    /// <summary>
    /// Process new messages (not changes to existing content)
    /// </summary>
    private async Task ProcessNewMessageAsync(RawMessage message)
    {
        _logger.LogInformation($"Processing new message {message.Id}");

        // Simple matching logic (instead of using MatcherService)
        var matchedAccount = await FindMatchingAccountSimpleAsync(message.MessageText, message.ChannelId);
        
        if (matchedAccount != null)
        {
            // Link message to existing account
            message.AccountId = matchedAccount.Id;
            
            // Update account info if needed
            matchedAccount.LastScrapedAt = DateTime.UtcNow;
            matchedAccount.UpdatedAt = DateTime.UtcNow;
            
            await _accountRepository.UpdateAsync(matchedAccount);
            
            _logger.LogInformation($"Message {message.Id} matched to existing account {matchedAccount.Id}");
        }
        else
        {
            // Try to parse and create new account
            var result = await ProcessRawMessageAsync(message.Id);
            if (result.Success)
            {
                _logger.LogInformation($"New account created from message {message.Id}: {result.AccountTitle}");
            }
            else
            {
                // Create notification for new unmatched message
                await CreateAdminNotificationAsync(
                    AdminNotificationType.NewMessage,
                    "New Unmatched Message",
                    "New message requires admin review",
                    NotificationPriority.Low,
                    message.Id,
                    "RawMessage",
                    new { 
                        MessagePreview = message.MessageText.Length > 100 
                            ? message.MessageText.Substring(0, 100) + "..." 
                            : message.MessageText
                    }
                );
                
                _logger.LogInformation($"New message {message.Id} requires admin review");
            }
        }
    }

    /// <summary>
    /// Simple account matching logic
    /// </summary>
    private async Task<Account?> FindMatchingAccountSimpleAsync(string messageText, int channelId)
    {
        try
        {
            // Extract potential account identifier from message (external message ID, title, etc.)
            var titleMatch = Regex.Match(messageText, @"^([^\n\r]+)", RegexOptions.Multiline);
            if (titleMatch.Success)
            {
                var title = titleMatch.Groups[1].Value.Trim();
                
                // Look for accounts with similar title in the same channel
                var accounts = await _accountRepository.GetByChannelIdAsync(channelId);
                return accounts.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.Title) && 
                    a.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in simple account matching for channel {ChannelId}", channelId);
            return null;
        }
    }

    /// <summary>
    /// Process all pending messages in batch
    /// </summary>
    public async Task ProcessAllPendingMessagesAsync()
    {
        try
        {
            _logger.LogInformation("Starting batch processing of pending messages");

            // Use repository method to get pending messages
            var pendingMessages = await _rawMessageRepo.GetPendingMessagesWithChannelAsync();
            var processedCount = 0;
            var failedCount = 0;

            foreach (var message in pendingMessages)
            {
                try
                {
                    await ProcessMessageAsync(message.Id);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process message {message.Id}");
                    failedCount++;
                }
            }

            _logger.LogInformation($"Batch processing completed. Processed: {processedCount}, Failed: {failedCount}");

            // Create summary notification for batch processing
            if (processedCount > 0 || failedCount > 0)
            {
                await CreateAdminNotificationAsync(
                    AdminNotificationType.SystemOperation,
                    "Batch Processing Completed",
                    $"Processed: {processedCount}, Failed: {failedCount}",
                    failedCount > processedCount / 2 ? NotificationPriority.High : NotificationPriority.Normal,
                    null,
                    "BatchProcessing",
                    new { ProcessedCount = processedCount, FailedCount = failedCount }
                );
            }

            // Simple state tracking (instead of using WorkerStateService)
            _logger.LogInformation("Last processing run completed at {Timestamp}. Processed: {ProcessedCount}, Failed: {FailedCount}", 
                DateTime.UtcNow, processedCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch processing");
            throw;
        }
    }

    /// <summary>
    /// Get processing statistics using repository methods
    /// </summary>
    public async Task<ProcessingStats> GetProcessingStatsAsync()
    {
        try
        {
            var stats = new ProcessingStats
            {
                TotalMessages = await _rawMessageRepo.CountByStatusAsync(RawMessageStatus.Pending) +
                               await _rawMessageRepo.CountByStatusAsync(RawMessageStatus.Processed) +
                               await _rawMessageRepo.CountByStatusAsync(RawMessageStatus.Failed) +
                               await _rawMessageRepo.CountByStatusAsync(RawMessageStatus.Processing) +
                               await _rawMessageRepo.CountByStatusAsync(RawMessageStatus.Ignored),
                PendingMessages = await _rawMessageRepo.GetPendingCountAsync(),
                ProcessedMessages = await _rawMessageRepo.GetProcessedCountAsync(),
                FailedMessages = await _rawMessageRepo.GetFailedCountAsync(),
                ChangesDetected = await _rawMessageRepo.CountByStatusAsync(RawMessageStatus.PendingChange),
                LastProcessingRun = DateTime.UtcNow // Simple fallback instead of WorkerStateService
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processing stats");
            throw;
        }
    }

    /// <summary>
    /// Create admin notification with proper entity structure
    /// </summary>
    private async Task CreateAdminNotificationAsync(
        AdminNotificationType type, 
        string title, 
        string message, 
        NotificationPriority priority,
        int? relatedEntityId = null,
        string? relatedEntityType = null,
        object? metadata = null)
    {
        try
        {
            var notification = new AdminNotification
            {
                Type = type,
                Title = title.Length > 200 ? title.Substring(0, 197) + "..." : title,
                Message = message.Length > 2000 ? message.Substring(0, 1997) + "..." : message,
                Priority = priority,
                IsRead = false,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType?.Length > 50 ? relatedEntityType.Substring(0, 50) : relatedEntityType,
                Metadata = metadata != null ? JsonSerializer.Serialize(metadata).Substring(0, Math.Min(1000, JsonSerializer.Serialize(metadata).Length)) : null,
                ExpiresAt = null, // Set expiration if needed based on notification type
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            _logger.LogInformation($"Admin Notification Created: [{priority}] {type} - {title}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin notification");
            // Don't throw - notification failure shouldn't break processing
        }
    }

    // ==================== EXISTING METHODS (Enhanced with Repository Methods) ====================

    /// <summary>
    /// Processes a raw message with change detection capabilities
    /// </summary>
    public async Task<ProcessingResult> ProcessRawMessageAsync(int rawMessageId)
    {
        var rawMessage = await _rawMessageRepo.GetByIdWithChannelAsync(rawMessageId);
        if (rawMessage == null)
        {
            _logger.LogWarning("RawMessage with ID {RawMessageId} not found", rawMessageId);
            return new ProcessingResult { Success = false, ErrorMessage = "Raw message not found." };
        }

        try
        {
            // ==================== STEP 1: GENERATE CONTENT HASH ====================
            var contentHash = _changeDetectionService.GenerateContentHash(rawMessage.MessageText);
            rawMessage.ContentHash = contentHash;
            
            _logger.LogDebug("Generated content hash for message {MessageId}: {Hash}", 
                rawMessageId, contentHash);

            // ==================== STEP 2: CHECK FOR CHANGES ====================
            var hasChanged = await _changeDetectionService.HasContentChangedAsync(
                rawMessage.ChannelId,
                rawMessage.ExternalMessageId.ToString(),
                contentHash);

            _logger.LogInformation("Change detection result for message {MessageId}: HasChanged={HasChanged}", 
                rawMessageId, hasChanged);

            if (hasChanged)
            {
                _logger.LogInformation("Content change detected for message {MessageId} from channel {ChannelId}",
                    rawMessageId, rawMessage.ChannelId);

                // Mark as change and get previous message for comparison
                rawMessage.IsChange = true;
                var previousMessage = await _changeDetectionService.GetPreviousMessageAsync(
                    rawMessage.ChannelId,
                    rawMessage.ExternalMessageId.ToString());

                if (previousMessage != null)
                {
                    rawMessage.PreviousMessageId = previousMessage.Id;
                    _logger.LogDebug("Found previous message {PreviousId} for comparison", previousMessage.Id);
                }

                // Parse both old and new data for detailed change detection
                var newParsedData = await _messageParser.ParseAccountMessageAsync(rawMessage.MessageText);
                ParsedAccountDto? oldParsedData = null;

                if (previousMessage != null)
                {
                    oldParsedData = await _messageParser.ParseAccountMessageAsync(previousMessage.MessageText);
                    _logger.LogDebug("Parsed old data for comparison from message {PreviousId}", previousMessage.Id);
                }

                // Detect specific changes
                var changeDetails = _changeDetectionService.DetectChanges(oldParsedData, newParsedData);

                _logger.LogInformation("Change analysis complete for message {MessageId}: {ChangeType}, {ChangeCount} changes detected", 
                    rawMessageId, changeDetails.ChangeType, changeDetails.Changes.Count);

                // Store change details
                rawMessage.ChangeDetails = changeDetails.ToJson();

                // Create admin notification if significant changes detected
                if (changeDetails.HasChanges && ShouldNotifyAdminOfChanges())
                {
                    await CreateChangeNotificationAsync(rawMessage, changeDetails, previousMessage);
                }

                // IMPROVED: Always process changes, but mark them appropriately
                rawMessage.Status = RawMessageStatus.Pending; // Process the change
                
                // Save the updated raw message BEFORE processing content
                await _rawMessageRepo.UpdateAsync(rawMessage);
                await _rawMessageRepo.SaveChangesAsync();
                
                _logger.LogInformation("Change marked and saved for message {MessageId}, proceeding with processing", rawMessageId);
            }
            else
            {
                // No change detected, but still process if it's a new message
                rawMessage.Status = RawMessageStatus.Pending;
                rawMessage.IsChange = false;
                
                await _rawMessageRepo.UpdateAsync(rawMessage);
                await _rawMessageRepo.SaveChangesAsync();
                
                _logger.LogDebug("No content change detected for message {MessageId}, processing normally", rawMessageId);
            }

            // ==================== STEP 3: CONTINUE PROCESSING ====================
            return await ProcessMessageContentAsync(rawMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing raw message {RawMessageId}", rawMessageId);

            // Mark as failed using repository method
            await _rawMessageRepo.MarkAsFailedAsync(rawMessageId, ex.Message);

            return new ProcessingResult
            {
                Success = false,
                ErrorMessage = $"Processing failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Processes the actual message content after change detection
    /// </summary>
    private async Task<ProcessingResult> ProcessMessageContentAsync(RawMessage rawMessage)
    {
        try
        {
            // Get channel info (already included in GetByIdWithChannelAsync)
            if (rawMessage.Channel == null)
            {
                _logger.LogError("Channel {ChannelId} not found for message {MessageId}",
                    rawMessage.ChannelId, rawMessage.Id);
                return new ProcessingResult { Success = false, ErrorMessage = "Channel not found" };
            }

            // Parse the message
            var parsedData = await _messageParser.ParseAccountMessageAsync(rawMessage.MessageText);
            if (parsedData == null)
            {
                _logger.LogDebug("Message {MessageId} could not be parsed as account data", rawMessage.Id);
                await _rawMessageRepo.MarkAsIgnoredAsync(rawMessage.Id);
                return new ProcessingResult { Success = true, ErrorMessage = "Message ignored - not account data" };
            }

            // Set the RawMessageId for tracking
            parsedData.RawMessageId = rawMessage.Id;

            // Create ViewModel for processing
            var viewModel = new ProcessMessageViewModel
            {
                RawMessageId = rawMessage.Id,
                Title = parsedData.Title ?? "",
                PricePs4 = parsedData.PricePs4,
                PricePs5 = parsedData.PricePs5,
                Region = parsedData.Region ?? "",
                SellerInfo = parsedData.SellerInfo ?? "",
                AdditionalInfo = parsedData.AdditionalInfo ?? "",
                GameTitles = parsedData.ExtractedGames ?? new List<string>()
            };

            // Process the account data
            var result = await ProcessAndSaveAccountAsync(viewModel);

            // Update message status based on result using repository methods
            if (result.Success)
            {
                await _rawMessageRepo.MarkAsProcessedAsync(rawMessage.Id, result.AccountId);
            }
            else
            {
                await _rawMessageRepo.MarkAsFailedAsync(rawMessage.Id, result.ErrorMessage ?? "Unknown error");
            }

            // If this was a change, log additional info
            if (rawMessage.IsChange)
            {
                _logger.LogInformation("Successfully processed CHANGE for message {MessageId}: {AccountTitle} (Account ID: {AccountId})", 
                    rawMessage.Id, result.AccountTitle, result.AccountId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message content for {MessageId}", rawMessage.Id);
            return new ProcessingResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Gets a preview of parsed data from a raw message without saving anything.
    /// </summary>
    public async Task<ParsedAccountDto?> GetParsedDataPreviewAsync(int rawMessageId)
    {
        var rawMessage = await _rawMessageRepo.GetByIdAsync(rawMessageId);
        if (rawMessage == null)
        {
            _logger.LogWarning("RawMessage with ID {Id} not found for preview.", rawMessageId);
            return null;
        }

        var parsedDto = await _messageParser.ParseAccountMessageAsync(rawMessage.MessageText);
        if (parsedDto == null)
        {
            _logger.LogWarning("MessageParser failed to parse message ID {Id}.", rawMessageId);
            return null;
        }

        // Enrich the DTO with additional info needed for the view
        parsedDto.RawMessageId = rawMessageId;

        var gamesWithStatus = new List<GamePreviewDto>();
        if (parsedDto.ExtractedGames != null)
            foreach (var title in parsedDto.ExtractedGames)
            {
                var gameExists = await _gameRepository.FindByTitleAsync(title) != null;
                gamesWithStatus.Add(new GamePreviewDto { Title = title, ExistsInDb = gameExists });
            }

        parsedDto.Games = gamesWithStatus;

        return parsedDto;
    }

    /// <summary>
    /// Processes and saves an account from the submitted view model data.
    /// It handles both creation of new accounts and updates to existing ones.
    /// </summary>
    public async Task<ProcessingResult> ProcessAndSaveAccountAsync(ProcessMessageViewModel viewModel)
    {
        var rawMessage = await _rawMessageRepo.GetByIdAsync(viewModel.RawMessageId);
        if (rawMessage == null)
            return new ProcessingResult { Success = false, ErrorMessage = "The original message could not be found." };

        var existingAccount =
            await _accountRepository.GetByExternalIdAsync(rawMessage.ChannelId,
                rawMessage.ExternalMessageId.ToString());

        try
        {
            if (existingAccount != null)
                return await UpdateExistingAccount(existingAccount, viewModel, rawMessage);
            else
                return await CreateNewAccount(viewModel, rawMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes to the database for message {MessageId}",
                viewModel.RawMessageId);
            return new ProcessingResult
            { Success = false, ErrorMessage = "A database error occurred while saving. See logs for details." };
        }
    }

    private async Task<ProcessingResult> CreateNewAccount(ProcessMessageViewModel viewModel, RawMessage rawMessage)
    {
        _logger.LogInformation("Creating new account from RawMessage ID: {RawMessageId}", rawMessage.Id);

        var gameEntities = await GetOrCreateGamesAsync(viewModel.GameTitles);

        var newAccount = new Account
        {
            ChannelId = rawMessage.ChannelId,
            ExternalId = rawMessage.ExternalMessageId.ToString(),
            Title = viewModel.Title,
            Description = rawMessage.MessageText,
            PricePs4 = viewModel.PricePs4,
            PricePs5 = viewModel.PricePs5,
            Region = viewModel.Region,
            StockStatus = StockStatus.InStock,
            SellerInfo = viewModel.SellerInfo,
            AdditionalInfo = viewModel.AdditionalInfo,
            CreatedAt = DateTime.UtcNow,
            LastScrapedAt = DateTime.UtcNow
        };

        foreach (var game in gameEntities)
            newAccount.AccountGames.Add(new AccountGame { Game = game });

        await _accountRepository.AddAsync(newAccount);
        await _accountRepository.SaveChangesAsync();

        // Create success notification
        await CreateAdminNotificationAsync(
            AdminNotificationType.AccountCreated,
            "New Account Created",
            $"New account '{newAccount.Title}' created successfully",
            NotificationPriority.Low,
            newAccount.Id,
            "Account",
            new { MessageId = rawMessage.Id, ChannelId = rawMessage.ChannelId }
        );

        _logger.LogInformation("Successfully created new account '{AccountTitle}' (ID: {AccountId})",
            newAccount.Title, newAccount.Id);

        return new ProcessingResult
        {
            Success = true,
            IsNewAccount = true,
            AccountId = newAccount.Id,
            AccountTitle = newAccount.Title,
            IsChange = rawMessage.IsChange
        };
    }

    private async Task<ProcessingResult> UpdateExistingAccount(Account existingAccount,
        ProcessMessageViewModel viewModel, RawMessage rawMessage)
    {
        _logger.LogInformation("Updating existing Account ID: {AccountId} (Change: {IsChange})", 
            existingAccount.Id, rawMessage.IsChange);

        existingAccount.Title = viewModel.Title;
        existingAccount.PricePs4 = viewModel.PricePs4;
        existingAccount.PricePs5 = viewModel.PricePs5;
        existingAccount.Region = viewModel.Region;
        existingAccount.SellerInfo = viewModel.SellerInfo;
        existingAccount.AdditionalInfo = viewModel.AdditionalInfo;
        existingAccount.UpdatedAt = DateTime.UtcNow;
        existingAccount.LastScrapedAt = DateTime.UtcNow;
        
        // Update description with new message content
        existingAccount.Description = rawMessage.MessageText;

        var gameEntities = await GetOrCreateGamesAsync(viewModel.GameTitles);

        existingAccount.AccountGames.Clear();
        foreach (var game in gameEntities)
            existingAccount.AccountGames.Add(new AccountGame { Account = existingAccount, Game = game });

        await _accountRepository.UpdateAsync(existingAccount);
        await _accountRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated account '{AccountTitle}' (ID: {AccountId}), Change: {IsChange}",
            existingAccount.Title, existingAccount.Id, rawMessage.IsChange);

        return new ProcessingResult
        {
            Success = true,
            IsNewAccount = false,
            AccountId = existingAccount.Id,
            AccountTitle = existingAccount.Title,
            IsChange = rawMessage.IsChange
        };
    }

    /// <summary>
    /// Retrieves existing games or prepares new Game entities to be saved.
    /// It does not save them immediately to allow for a single transaction.
    /// </summary>
    private async Task<List<Game>> GetOrCreateGamesAsync(List<string> titles)
    {
        var gameEntities = new List<Game>();
        if (titles == null || !titles.Any()) return gameEntities;

        var distinctTitles =
            titles.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var title in distinctTitles)
        {
            var game = await _gameRepository.FindByTitleAsync(title);
            if (game == null)
                // Create a new entity, but don't add it to the context here.
                // EF Core will detect it as a new entity when it's part of the Account's navigation property.
                game = new Game { Title = title };
            gameEntities.Add(game);
        }

        return gameEntities;
    }

    // ==================== CHANGE DETECTION HELPER METHODS ====================

    private async Task CreateChangeNotificationAsync(RawMessage changedMessage, ChangeDetails changes, RawMessage? previousMessage)
    {
        try
        {
            var channel = await _channelRepository.GetByIdAsync(changedMessage.ChannelId);
            var channelName = channel?.Name ?? "Unknown Channel";

            var notificationType = changes.ChangeType switch
            {
                ChangeType.Deleted => AdminNotificationType.AccountDeleted,
                ChangeType.PriceChanged => AdminNotificationType.PriceChanged,
                ChangeType.GamesChanged => AdminNotificationType.AccountUpdated,
                _ => AdminNotificationType.AccountUpdated
            };

            var priority = changes.ChangeType switch
            {
                ChangeType.Deleted => NotificationPriority.High,
                ChangeType.PriceChanged => NotificationPriority.Normal,
                _ => NotificationPriority.Low
            };

            await CreateAdminNotificationAsync(
                notificationType,
                $"Account Updated in {channelName}",
                FormatChangeMessage(changes, changedMessage.ExternalMessageId),
                priority,
                changedMessage.Id,
                "RawMessage",
                new
                {
                    ChangeType = changes.ChangeType.ToString(),
                    ChangeCount = changes.Changes.Count,
                    ExternalMessageId = changedMessage.ExternalMessageId,
                    ChannelId = changedMessage.ChannelId
                }
            );

            _logger.LogInformation("Created admin notification for change in message {MessageId}", changedMessage.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating change notification for message {MessageId}", changedMessage.Id);
            // Don't throw - notification failure shouldn't break processing
        }
    }

    private string FormatChangeMessage(ChangeDetails changes, long externalId)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Account {externalId} has been updated:");
        sb.AppendLine($"Change Type: {changes.ChangeType}");
        sb.AppendLine();

        foreach (var change in changes.Changes.Take(5)) // Limit to first 5 changes
        {
            sb.AppendLine($"• {change.Field}: {change.OldValue} → {change.NewValue}");
        }

        if (changes.Changes.Count > 5)
        {
            sb.AppendLine($"• ... and {changes.Changes.Count - 5} more changes");
        }

        return sb.ToString();
    }

    private bool ShouldAutoProcessChanges()
    {
        var section = _configuration.GetSection("ChangeDetectionSettings:AutoProcessChanges");
        return !section.Exists() || bool.Parse(section.Value ?? "true"); // Default to true
    }

    private bool ShouldNotifyAdminOfChanges()
    {
        var section = _configuration.GetSection("ChangeDetectionSettings:NotifyAdminOnChanges");
        return !section.Exists() || bool.Parse(section.Value ?? "true"); // Default to true
    }
}

/// <summary>
/// Statistics class for processing operations
/// </summary>
public class ProcessingStats
{
    public int TotalMessages { get; set; }
    public int PendingMessages { get; set; }
    public int ProcessedMessages { get; set; }
    public int FailedMessages { get; set; }
    public int ChangesDetected { get; set; }
    public DateTime? LastProcessingRun { get; set; }
}
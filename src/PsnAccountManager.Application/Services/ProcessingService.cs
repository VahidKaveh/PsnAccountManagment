using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;
using System.Text;

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

    // ==================== NEW SERVICES FOR CHANGE DETECTION ====================
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

    /// <summary>
    /// Processes a raw message with change detection capabilities
    /// </summary>
    public async Task<ProcessingResult> ProcessRawMessageAsync(int rawMessageId)
    {
        var rawMessage = await _rawMessageRepo.GetByIdAsync(rawMessageId);
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

            // ==================== STEP 2: CHECK FOR CHANGES ====================
            var hasChanged = await _changeDetectionService.HasContentChangedAsync(
                rawMessage.ChannelId,
                rawMessage.ExternalMessageId.ToString(),
                contentHash);

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
                }

                // Parse both old and new data for detailed change detection
                var newParsedData = await _messageParser.ParseAccountMessageAsync(rawMessage.MessageText);
                ParsedAccountDto? oldParsedData = null;

                if (previousMessage != null)
                {
                    oldParsedData = await _messageParser.ParseAccountMessageAsync(previousMessage.MessageText);
                }

                // Detect specific changes
                var changeDetails = _changeDetectionService.DetectChanges(oldParsedData, newParsedData);

                // Store change details
                rawMessage.ChangeDetails = changeDetails.ToJson();

                // Create admin notification if significant changes detected
                if (changeDetails.HasChanges && ShouldNotifyAdminOfChanges())
                {
                    await CreateChangeNotificationAsync(rawMessage, changeDetails, previousMessage);
                }

                // Determine processing strategy
                if (ShouldAutoProcessChanges())
                {
                    rawMessage.Status = RawMessageStatus.Pending; // Process normally
                    _logger.LogDebug("Auto-processing change for message {MessageId}", rawMessageId);
                }
                else
                {
                    rawMessage.Status = RawMessageStatus.PendingChange; // Queue for manual review
                    _logger.LogInformation("Change queued for manual review: message {MessageId}", rawMessageId);
                }
            }
            else
            {
                // No change detected, process normally
                rawMessage.Status = RawMessageStatus.Pending;
                _logger.LogDebug("No content change detected for message {MessageId}", rawMessageId);
            }

            // Save the updated raw message
            _rawMessageRepo.Update(rawMessage);
            await _rawMessageRepo.SaveChangesAsync();

            // ==================== STEP 3: CONTINUE PROCESSING IF APPROPRIATE ====================
            if (rawMessage.Status == RawMessageStatus.Pending)
            {
                return await ProcessMessageContentAsync(rawMessage);
            }

            return new ProcessingResult
            {
                Success = true,
                ErrorMessage = rawMessage.Status == RawMessageStatus.PendingChange
                    ? "Change detected and queued for review"
                    : "Message processed successfully",
                IsChange = rawMessage.IsChange
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing raw message {RawMessageId}", rawMessageId);

            rawMessage.Status = RawMessageStatus.Failed;
            _rawMessageRepo.Update(rawMessage);
            await _rawMessageRepo.SaveChangesAsync();

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
            // Get channel info
            var channel = await _channelRepository.GetByIdAsync(rawMessage.ChannelId);
            if (channel == null)
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
                rawMessage.Status = RawMessageStatus.Ignored;
                _rawMessageRepo.Update(rawMessage);
                await _rawMessageRepo.SaveChangesAsync();
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

            // Update message status based on result
            rawMessage.Status = result.Success ? RawMessageStatus.Processed : RawMessageStatus.Failed;
            _rawMessageRepo.Update(rawMessage);
            await _rawMessageRepo.SaveChangesAsync();

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
                return await UpdateExistingAccount(existingAccount, viewModel);
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

        rawMessage.Status = RawMessageStatus.Processed;
        _rawMessageRepo.Update(rawMessage);

        await _rawMessageRepo.SaveChangesAsync();

        _logger.LogInformation("Successfully created new account '{AccountTitle}' (ID: {AccountId})",
            newAccount.Title, newAccount.Id);

        return new ProcessingResult
        {
            Success = true,
            IsNewAccount = true,
            AccountId = newAccount.Id,
            AccountTitle = newAccount.Title
        };
    }

    private async Task<ProcessingResult> UpdateExistingAccount(Account existingAccount,
        ProcessMessageViewModel viewModel)
    {
        _logger.LogInformation("Updating existing Account ID: {AccountId}", existingAccount.Id);

        existingAccount.Title = viewModel.Title;
        existingAccount.PricePs4 = viewModel.PricePs4;
        existingAccount.PricePs5 = viewModel.PricePs5;
        existingAccount.Region = viewModel.Region;
        existingAccount.SellerInfo = viewModel.SellerInfo;
        existingAccount.AdditionalInfo = viewModel.AdditionalInfo;
        existingAccount.UpdatedAt = DateTime.UtcNow;
        existingAccount.LastScrapedAt = DateTime.UtcNow;

        var gameEntities = await GetOrCreateGamesAsync(viewModel.GameTitles);

        existingAccount.AccountGames.Clear();
        foreach (var game in gameEntities)
            existingAccount.AccountGames.Add(new AccountGame { Account = existingAccount, Game = game });

        _accountRepository.Update(existingAccount);

        var rawMessage = await _rawMessageRepo.GetByIdAsync(viewModel.RawMessageId);
        if (rawMessage != null)
        {
            rawMessage.Status = RawMessageStatus.Processed;
            _rawMessageRepo.Update(rawMessage);
        }

        await _rawMessageRepo.SaveChangesAsync();

        _logger.LogInformation("Successfully updated account '{AccountTitle}' (ID: {AccountId})",
            existingAccount.Title, existingAccount.Id);

        return new ProcessingResult
        {
            Success = true,
            IsNewAccount = false,
            AccountId = existingAccount.Id,
            AccountTitle = existingAccount.Title
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

            var notification = new AdminNotification
            {
                Type = AdminNotificationType.AccountChanged,
                Title = $"Account Updated in {channelName}",
                Message = FormatChangeMessage(changes, changedMessage.ExternalMessageId),
                RelatedEntityId = changedMessage.Id,
                RelatedEntityType = "RawMessage",
                Priority = DetermineNotificationPriority(changes),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

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

    private NotificationPriority DetermineNotificationPriority(ChangeDetails changes)
    {
        // High priority for sold status or significant price changes
        if (changes.ChangeType == ChangeType.Deleted)
            return NotificationPriority.High;

        if (changes.ChangeType == ChangeType.PriceChanged)
            return NotificationPriority.Normal;

        return NotificationPriority.Low;
    }

    private bool ShouldAutoProcessChanges()
    {
        var section = _configuration.GetSection("ChangeDetectionSettings:AutoProcessChanges");
        return section.Exists() && bool.Parse(section.Value ?? "false");
    }

    private bool ShouldNotifyAdminOfChanges()
    {
        var section = _configuration.GetSection("ChangeDetectionSettings:NotifyAdminOnChanges");
        return !section.Exists() || bool.Parse(section.Value ?? "true");
    }


}

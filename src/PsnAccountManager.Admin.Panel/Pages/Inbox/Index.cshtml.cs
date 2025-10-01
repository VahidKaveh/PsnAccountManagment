using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Admin.Panel.Pages.Inbox;

public class IndexModel : PageModel
{
    private readonly IRawMessageRepository _rawMessageRepository;
    private readonly IProcessingService _processingService;
    private readonly IAdminNotificationRepository _notificationRepository;
    private readonly IChannelRepository _channelRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ILearningDataRepository _learningDataRepository;
    private readonly ILogger<IndexModel> _logger;

    // Properties for the view
    public List<RawMessage> Messages { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 10;
    public int TotalPendingMessages { get; set; }

    public IndexModel(
        IRawMessageRepository rawMessageRepository,
        IProcessingService processingService,
        ILogger<IndexModel> logger,
        IAdminNotificationRepository notificationRepository,
        IChannelRepository channelRepository,
        IGameRepository gameRepository,
        IAccountRepository accountRepository,
        ILearningDataRepository learningDataRepository)
    {
        _rawMessageRepository = rawMessageRepository ?? throw new ArgumentNullException(nameof(rawMessageRepository));
        _processingService = processingService ?? throw new ArgumentNullException(nameof(processingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _channelRepository = channelRepository ?? throw new ArgumentNullException(nameof(channelRepository));
        _gameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _learningDataRepository = learningDataRepository ?? throw new ArgumentNullException(nameof(learningDataRepository));
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Loading inbox page {CurrentPage}", CurrentPage);

            var pendingMessages = await _rawMessageRepository.GetPendingMessagesWithChannelAsync();
            var pendingMessagesQuery = pendingMessages.AsQueryable();

            TotalPendingMessages = pendingMessagesQuery.Count();
            TotalPages = (int)Math.Ceiling(TotalPendingMessages / (double)PageSize);

            // Validate current page
            if (CurrentPage > TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
                return RedirectToPage(new { CurrentPage });
            }

            Messages = pendingMessagesQuery
                .OrderByDescending(m => m.ReceivedAt)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            _logger.LogInformation(
                "Loaded {MessageCount} messages for page {CurrentPage} of {TotalPages}. Total pending: {TotalPending}",
                Messages.Count, CurrentPage, TotalPages, TotalPendingMessages);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inbox page");
            StatusMessage = "An error occurred while loading messages. Please try again.";
            Messages = new List<RawMessage>();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostIgnoreAsync(int id)
    {
        try
        {
            _logger.LogInformation("Attempting to ignore message {MessageId}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Invalid message ID: {MessageId}", id);
                StatusMessage = "Invalid message ID provided.";
                return RedirectToPage();
            }

            var message = await _rawMessageRepository.GetByIdAsync(id);
            if (message == null)
            {
                _logger.LogWarning("Message {MessageId} not found", id);
                StatusMessage = $"Message with ID {id} not found.";
                return RedirectToPage();
            }

            if (message.Status == RawMessageStatus.Ignored)
            {
                _logger.LogInformation("Message {MessageId} is already ignored", id);
                StatusMessage = $"Message ID {id} is already ignored.";
                return RedirectToPage();
            }

            message.Status = RawMessageStatus.Ignored;
            message.ProcessedAt = DateTime.UtcNow;
            message.ProcessingResult = "Manually ignored by admin";

            _rawMessageRepository.Update(message);
            await _rawMessageRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully ignored message {MessageId}", id);
            StatusMessage = $"Message ID {id} has been ignored successfully.";

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ignoring message {MessageId}", id);
            StatusMessage = $"An error occurred while ignoring message ID {id}. Please try again.";
            return RedirectToPage();
        }
    }

    /// <summary>
    /// Handler for getting parsed data preview (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetProcessPreviewAsync(int id)
    {
        try
        {
            _logger.LogInformation("Processing preview for message {MessageId}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Invalid message ID for preview: {MessageId}", id);
                return new JsonResult(new
                {
                    success = false,
                    message = "Invalid message ID provided."
                });
            }

            var rawMessage = await _rawMessageRepository.GetByIdAsync(id);
            if (rawMessage == null)
            {
                _logger.LogWarning("Message {MessageId} not found", id);
                return new JsonResult(new
                {
                    success = false,
                    message = $"Message with ID {id} not found."
                });
            }

            if (rawMessage.Status != RawMessageStatus.Pending)
            {
                _logger.LogWarning("Message {MessageId} has status {Status}, expected Pending", 
                    id, rawMessage.Status);
                return new JsonResult(new
                {
                    success = false,
                    message = $"Message {id} is not in pending status. Current status: {rawMessage.Status}"
                });
            }

            var channel = await _channelRepository.GetChannelWithProfileAsync(rawMessage.ChannelId);
            if (channel == null)
            {
                _logger.LogWarning("Channel {ChannelId} not found for message {MessageId}", 
                    rawMessage.ChannelId, id);
                return new JsonResult(new
                {
                    success = false,
                    message = $"Channel associated with message {id} not found."
                });
            }

            if (channel.ParsingProfile == null)
            {
                _logger.LogWarning("No parsing profile for channel {ChannelId}, message {MessageId}", 
                    rawMessage.ChannelId, id);
                return new JsonResult(new
                {
                    success = false,
                    message = $"No parsing profile is assigned to channel '{channel.Name}'. Please configure a parsing profile first."
                });
            }

            if (!channel.ParsingProfile.Rules.Any())
            {
                _logger.LogWarning("Parsing profile {ProfileId} has no rules for channel {ChannelId}", 
                    channel.ParsingProfile.Id, channel.Id);
                return new JsonResult(new
                {
                    success = false,
                    message = $"Parsing profile '{channel.ParsingProfile.Name}' has no rules configured. Please add parsing rules."
                });
            }

            // Get parsed data preview
            var parsedData = await _processingService.GetParsedDataPreviewAsync(id);
            if (parsedData == null)
            {
                _logger.LogWarning("Failed to parse message {MessageId} with profile {ProfileId}", 
                    id, channel.ParsingProfile.Id);
                return new JsonResult(new
                {
                    success = false,
                    message = "Failed to parse the message. Please check the parsing profile rules and message format."
                });
            }

            // Check which games exist in database
            if (parsedData.ExtractedGames != null && parsedData.ExtractedGames.Any())
            {
                foreach (var game in parsedData.ExtractedGames)
                {
                    var firstGame = parsedData.ExtractedGames?.FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstGame))
                    {
                        var gameTitle = firstGame;  
                        var gameEntity = await _gameRepository.FindByTitleAsync(firstGame);
                        var exists = gameEntity != null;
                    }
                }
            }

            _logger.LogInformation("Successfully generated preview for message {MessageId}", id);

            var gamesList = new List<object>();
            foreach (var title in parsedData.ExtractedGames ?? new List<string>())
            {
                var exists = (await _gameRepository.FindByTitleAsync(title)) != null;
                gamesList.Add(new { title, existsInDb = exists });
            }
            return new JsonResult(new 
            { 
                success = true, 
                data = new
                {
                    rawMessageId = id,
                    title = parsedData.Title,
                    pricePs4 = parsedData.PricePs4,
                    pricePs5 = parsedData.PricePs5,
                    region = parsedData.Region,
                    games = gamesList,
                    fullDescription = parsedData.FullDescription ?? rawMessage.MessageText
                }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argument error during preview for message {MessageId}", id);
            return new JsonResult(new
            {
                success = false,
                message = "Invalid data provided for processing."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during preview for message {MessageId}", id);
            return new JsonResult(new
            {
                success = false,
                message = "Operation cannot be completed. Please check the message and profile configuration."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during preview for message {MessageId}", id);
            return new JsonResult(new
            {
                success = false,
                message = "An unexpected error occurred during processing. Please try again or contact support."
            });
        }
    }

    /// <summary>
    /// Handler for processing and saving account with manual corrections support (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostProcessAndSaveAsync([FromBody] ProcessMessageViewModel viewModel)
    {
        try
        {
            _logger.LogInformation("Processing and saving account for message {MessageId}", viewModel?.RawMessageId);

            // Validation
            if (viewModel == null)
            {
                _logger.LogWarning("ProcessMessageViewModel is null");
                return new JsonResult(new
                {
                    success = false,
                    message = "No data provided for processing."
                });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Model validation failed for message {MessageId}. Errors: {Errors}",
                    viewModel.RawMessageId, string.Join(", ", errors));

                return new JsonResult(new
                {
                    success = false,
                    message = "Invalid data submitted: " + string.Join(", ", errors)
                });
            }

            // Additional validation
            if (string.IsNullOrWhiteSpace(viewModel.Title))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Account title is required."
                });
            }

            if (viewModel.RawMessageId <= 0)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Invalid message ID."
                });
            }

            // Check if message exists and is in pending status
            var rawMessage = await _rawMessageRepository.GetByIdAsync(viewModel.RawMessageId);
            if (rawMessage == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Message with ID {viewModel.RawMessageId} not found."
                });
            }

            if (rawMessage.Status != RawMessageStatus.Pending)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Message {viewModel.RawMessageId} is not in pending status."
                });
            }

            // Process manual corrections and create learning data
            if (viewModel.ManualCorrections != null && viewModel.ManualCorrections.Any())
            {
                await ProcessManualCorrectionsAsync(viewModel.ManualCorrections, rawMessage);
            }

            // Process and save account
            var result = await _processingService.ProcessAndSaveAccountAsync(viewModel);

            if (result == null)
            {
                _logger.LogError("ProcessingService returned null for message {MessageId}", viewModel.RawMessageId);
                return new JsonResult(new
                {
                    success = false,
                    message = "Processing service returned no result. Please contact support."
                });
            }

            if (result.Success)
            {
                await HandleSuccessfulProcessing(result, viewModel.RawMessageId);

                // Log manual corrections
                if (viewModel.ManualCorrections != null && viewModel.ManualCorrections.Any())
                {
                    _logger.LogInformation(
                        "Processed {CorrectionCount} manual corrections for message {MessageId}: {Corrections}",
                        viewModel.ManualCorrections.Count,
                        viewModel.RawMessageId,
                        string.Join(", ", viewModel.ManualCorrections.Select(c => $"{c.EntityType}:{c.EntityValue}")));
                }

                _logger.LogInformation("Successfully processed message {MessageId}. Account: {AccountTitle}",
                    viewModel.RawMessageId, result.AccountTitle);

                return new JsonResult(new { success = true });
            }
            else
            {
                var errorMessage = GetErrorMessage(result);
                _logger.LogWarning("Processing failed for message {MessageId}. Error: {Error}",
                    viewModel.RawMessageId, errorMessage);

                return new JsonResult(new
                {
                    success = false,
                    message = errorMessage
                });
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argument error during processing for message {MessageId}", viewModel?.RawMessageId);
            return new JsonResult(new
            {
                success = false,
                message = "Invalid data provided for processing."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during processing for message {MessageId}", viewModel?.RawMessageId);
            return new JsonResult(new
            {
                success = false,
                message = "Operation cannot be completed at this time. Please try again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during processing for message {MessageId}", viewModel?.RawMessageId);
            return new JsonResult(new
            {
                success = false,
                message = "An unexpected error occurred while processing. Please try again or contact support."
            });
        }
    }

    /// <summary>
    /// Process manual corrections and store as learning data
    /// </summary>
    private async Task ProcessManualCorrectionsAsync(List<ManualCorrectionDto> corrections, RawMessage rawMessage)
    {
        try
        {
            foreach (var correction in corrections)
            {
                // Create learning data entry
                var learningData = new LearningData
                {
                    ChannelId = rawMessage.ChannelId,
                    RawMessageId = rawMessage.Id,
                    EntityType = correction.EntityType,
                    EntityValue = correction.EntityValue,
                    OriginalText = rawMessage.MessageText,
                    TextContext = ExtractContext(rawMessage.MessageText, correction.EntityValue),
                    ConfidenceLevel = 5, // Manual corrections are high confidence
                    IsManualCorrection = true,
                    CreatedBy = User.Identity?.Name ?? "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                await _learningDataRepository.AddAsync(learningData);
            }

            await _learningDataRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Stored {Count} manual corrections as learning data for message {MessageId}",
                corrections.Count, rawMessage.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing manual corrections as learning data for message {MessageId}", 
                rawMessage.Id);
            // Don't throw - allow the main process to continue
        }
    }

    /// <summary>
    /// Extract context around a value in text
    /// </summary>
    private string ExtractContext(string text, string value, int contextLength = 50)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value))
            return text;

        var index = text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
            return text.Length > 100 ? text.Substring(0, 100) : text;

        var start = Math.Max(0, index - contextLength);
        var end = Math.Min(text.Length, index + value.Length + contextLength);

        return text.Substring(start, end - start);
    }

    /// <summary>
    /// Get error message from processing result using reflection
    /// </summary>
    private string GetErrorMessage(ProcessingResult result)
    {
        var resultType = result.GetType();

        // Try ErrorMessage property
        var errorMessageProp = resultType.GetProperty("ErrorMessage");
        if (errorMessageProp != null)
        {
            var errorMessage = errorMessageProp.GetValue(result) as string;
            if (!string.IsNullOrEmpty(errorMessage))
                return errorMessage;
        }

        // Try Message property
        var messageProp = resultType.GetProperty("Message");
        if (messageProp != null)
        {
            var message = messageProp.GetValue(result) as string;
            if (!string.IsNullOrEmpty(message))
                return message;
        }

        // Try Error property
        var errorProp = resultType.GetProperty("Error");
        if (errorProp != null)
        {
            var error = errorProp.GetValue(result) as string;
            if (!string.IsNullOrEmpty(error))
                return error;
        }

        return "An error occurred while saving the account.";
    }

    /// <summary>
    /// Handle successful processing result
    /// </summary>
    private async Task HandleSuccessfulProcessing(ProcessingResult result, int messageId)
    {
        try
        {
            if (result.IsNewAccount)
            {
                StatusMessage = $"Message ID {messageId} processed and new account '{result.AccountTitle}' created successfully!";
                _logger.LogInformation("New account '{AccountTitle}' created from message {MessageId}", 
                    result.AccountTitle, messageId);
            }
            else
            {
                if (result.DetectedChanges != null && result.DetectedChanges.Any())
                {
                    var changesSummary = string.Join(", ", result.DetectedChanges.Select(c => c.FieldName));
                    var message = $"Account '{result.AccountTitle}' was updated: {changesSummary}";

                    var linkUrl = Url.Page("/Accounts/Details", new { id = result.AccountId });

                    var notification = new AdminNotification
                    {
                        Message = message,
                        LinkUrl = linkUrl,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    await _notificationRepository.AddAsync(notification);
                    await _notificationRepository.SaveChangesAsync();

                    StatusMessage = $"Message ID {messageId} processed. {message}";

                    _logger.LogInformation(
                        "Account '{AccountTitle}' updated from message {MessageId}. Changes: {Changes}",
                        result.AccountTitle, messageId, changesSummary);
                }
                else
                {
                    StatusMessage = $"Message ID {messageId} processed. No changes detected for account '{result.AccountTitle}'.";

                    _logger.LogInformation(
                        "Message {MessageId} processed but no changes for account '{AccountTitle}'",
                        messageId, result.AccountTitle);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling successful processing for message {MessageId}", messageId);
            StatusMessage = $"Message ID {messageId} processed successfully, but there was an issue with notifications.";
        }
    }
}

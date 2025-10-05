using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;

namespace PsnAccountManager.Admin.Panel.Pages.Inbox;

public class IndexModel : PageModel
{
    private readonly IRawMessageRepository _rawMessageRepository;
    private readonly IProcessingService _processingService;
    private readonly ILogger<IndexModel> _logger;

    // Properties
    public List<RawMessage> Messages { get; set; } = new();
    public List<RawMessage> PendingChanges { get; set; } = new();
    public int TotalPendingChanges { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 10;
    public int TotalPendingMessages { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "messages";

    public IndexModel(
        IRawMessageRepository rawMessageRepository,
        IProcessingService processingService,
        ILogger<IndexModel> logger)
    {
        _rawMessageRepository = rawMessageRepository ?? throw new ArgumentNullException(nameof(rawMessageRepository));
        _processingService = processingService ?? throw new ArgumentNullException(nameof(processingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync(string tab = "messages")
    {
        ActiveTab = tab.ToLower();
        ViewData["ActiveTab"] = ActiveTab;

        try
        {
            _logger.LogInformation("Loading inbox page {CurrentPage} for tab {Tab}", CurrentPage, ActiveTab);

            if (ActiveTab == "changes")
            {
                await LoadPendingChanges();
            }
            else
            {
                await LoadPendingMessages();
            }

            await LoadTabCounts();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inbox page for tab {Tab}", ActiveTab);
            StatusMessage = "An error occurred while loading messages. Please try again.";
            Messages = new List<RawMessage>();
            PendingChanges = new List<RawMessage>();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostIgnoreAsync(int id)
    {
        try
        {
            _logger.LogInformation("Attempting to ignore message {MessageId}", id);

            var message = await _rawMessageRepository.GetByIdAsync(id);
            if (message == null)
            {
                StatusMessage = $"Message with ID {id} not found.";
                return RedirectToPage(new { tab = ActiveTab, CurrentPage });
            }

            message.Status = RawMessageStatus.Ignored;
            message.ProcessedAt = DateTime.UtcNow;
            _rawMessageRepository.Update(message);
            await _rawMessageRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully ignored message {MessageId}", id);
            StatusMessage = $"Message ID {id} has been ignored successfully.";

            return RedirectToPage(new { tab = ActiveTab, CurrentPage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ignoring message {MessageId}", id);
            StatusMessage = $"An error occurred while ignoring message ID {id}. Please try again.";
            return RedirectToPage(new { tab = ActiveTab, CurrentPage });
        }
    }

    public async Task<IActionResult> OnPostApproveChangeAsync(int messageId)
    {
        try
        {
            _logger.LogInformation("Attempting to approve change for message {MessageId}", messageId);

            var message = await _rawMessageRepository.GetByIdAsync(messageId);
            if (message == null || message.Status != RawMessageStatus.PendingChange)
            {
                StatusMessage = "Message not found or not in pending change status.";
                return RedirectToPage(new { tab = "changes", CurrentPage });
            }

            message.Status = RawMessageStatus.Pending;
            _rawMessageRepository.Update(message);
            await _rawMessageRepository.SaveChangesAsync();

            StatusMessage = $"Change approved and queued for processing.";
            _logger.LogInformation("Change approved for message {MessageId}", messageId);

            return RedirectToPage(new { tab = "changes", CurrentPage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving change for message {MessageId}", messageId);
            StatusMessage = "Error approving change. Please try again.";
            return RedirectToPage(new { tab = "changes", CurrentPage });
        }
    }

    public async Task<IActionResult> OnPostIgnoreChangeAsync(int messageId)
    {
        try
        {
            _logger.LogInformation("Attempting to ignore change for message {MessageId}", messageId);

            var message = await _rawMessageRepository.GetByIdAsync(messageId);
            if (message == null || message.Status != RawMessageStatus.PendingChange)
            {
                StatusMessage = "Message not found or not in pending change status.";
                return RedirectToPage(new { tab = "changes", CurrentPage });
            }

            message.Status = RawMessageStatus.Ignored;
            message.ProcessedAt = DateTime.UtcNow;
            _rawMessageRepository.Update(message);
            await _rawMessageRepository.SaveChangesAsync();

            StatusMessage = "Change ignored successfully.";
            _logger.LogInformation("Change ignored for message {MessageId}", messageId);

            return RedirectToPage(new { tab = "changes", CurrentPage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ignoring change for message {MessageId}", messageId);
            StatusMessage = "Error ignoring change. Please try again.";
            return RedirectToPage(new { tab = "changes", CurrentPage });
        }
    }

    public async Task<IActionResult> OnGetProcessPreviewAsync(int id)
    {
        try
        {
            _logger.LogInformation("Processing preview for message {MessageId}", id);

            var parsedData = await _processingService.GetParsedDataPreviewAsync(id);
            if (parsedData == null)
            {
                _logger.LogWarning("Failed to parse message {MessageId}", id);
                return new JsonResult(new
                {
                    success = false,
                    message = "Failed to parse the message. Check message format or parsing logic."
                });
            }

            _logger.LogInformation("Successfully generated preview for message {MessageId}", id);
            return new JsonResult(new { success = true, data = parsedData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during preview for message {MessageId}", id);
            return new JsonResult(new
            {
                success = false,
                message = "An unexpected error occurred during processing."
            });
        }
    }

    public async Task<IActionResult> OnPostProcessAndSaveAsync([FromBody] ProcessMessageViewModel viewModel)
    {
        try
        {
            _logger.LogInformation("Processing and saving account for message {MessageId}", viewModel?.RawMessageId);

            if (viewModel == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "No data received."
                });
            }

            ModelState.Clear();

            if (viewModel.RawMessageId <= 0)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Invalid message ID."
                });
            }

            if (string.IsNullOrWhiteSpace(viewModel.Title))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Title is required."
                });
            }

            var result = await _processingService.ProcessAndSaveAccountAsync(viewModel);

            if (result != null && result.Success)
            {
                // دریافت پیام مرتبط
                var rawMessage = await _rawMessageRepository.GetByIdAsync(viewModel.RawMessageId);
                if (rawMessage != null)
                {
                    rawMessage.Status = RawMessageStatus.Processed; 
                    rawMessage.ProcessedAt = DateTime.UtcNow;
                    _rawMessageRepository.Update(rawMessage);
                    await _rawMessageRepository.SaveChangesAsync();
                }
                StatusMessage = $"Message ID {viewModel.RawMessageId} processed. Account '{result.AccountTitle}' saved.";
                return new JsonResult(new { success = true });
            }
            else
            {
                var errorMessage = result?.ErrorMessage ?? "An unknown processing error occurred.";
                _logger.LogWarning("Processing failed for message {MessageId}. Error: {Error}",
                    viewModel.RawMessageId, errorMessage);
                return new JsonResult(new
                {
                    success = false,
                    message = errorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during processing for message {MessageId}", viewModel?.RawMessageId);
            return new JsonResult(new
            {
                success = false,
                message = $"An unexpected error occurred: {ex.Message}"
            });
        }
    }

    private async Task LoadPendingMessages()
    {
        var pendingMessages = await _rawMessageRepository.GetPendingMessagesWithChannelAsync();
        var pendingMessagesQuery = pendingMessages.AsQueryable();

        TotalPendingMessages = pendingMessagesQuery.Count();
        TotalPages = (int)Math.Ceiling(TotalPendingMessages / (double)PageSize);

        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
        }

        Messages = pendingMessagesQuery
            .OrderByDescending(m => m.ReceivedAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        _logger.LogInformation(
            "Loaded {MessageCount} messages for page {CurrentPage} of {TotalPages}. Total pending: {TotalPending}",
            Messages.Count, CurrentPage, TotalPages, TotalPendingMessages);
    }

    private async Task LoadPendingChanges()
    {
        var allChanges = await _rawMessageRepository.GetByStatusAsync(RawMessageStatus.PendingChange, 0, 1000);
        var allChangesList = allChanges.ToList();
        TotalPendingChanges = allChangesList.Count;
        TotalPages = (int)Math.Ceiling(TotalPendingChanges / (double)PageSize);

        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
        }

        var skip = (CurrentPage - 1) * PageSize;
        PendingChanges = allChangesList
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(PageSize)
            .ToList();

        _logger.LogInformation(
            "Loaded {ChangeCount} changes for page {CurrentPage} of {TotalPages}. Total pending changes: {TotalPending}",
            PendingChanges.Count, CurrentPage, TotalPages, TotalPendingChanges);
    }

    private async Task LoadTabCounts()
    {
        if (TotalPendingMessages == 0)
            TotalPendingMessages = await _rawMessageRepository.CountByStatusAsync(RawMessageStatus.Pending);

        if (TotalPendingChanges == 0)
            TotalPendingChanges = await _rawMessageRepository.CountByStatusAsync(RawMessageStatus.PendingChange);
    }

    public ChangeDetails? GetChangeDetails(RawMessage message)
    {
        if (string.IsNullOrEmpty(message.ChangeDetails))
            return null;

        return ChangeDetails.FromJson(message.ChangeDetails);
    }

    public string GetChangeTypeBadgeClass(ChangeType changeType)
    {
        return changeType switch
        {
            ChangeType.New => "badge-success",
            ChangeType.Modified => "badge-info",
            ChangeType.PriceChanged => "badge-warning",
            ChangeType.GamesChanged => "badge-primary",
            ChangeType.RegionChanged => "badge-secondary",
            ChangeType.Deleted => "badge-danger",
            _ => "badge-light"
        };
    }
}

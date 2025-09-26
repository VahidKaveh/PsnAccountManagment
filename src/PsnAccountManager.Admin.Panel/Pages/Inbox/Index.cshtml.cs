using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Application.Interfaces; 
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels; 


namespace PsnAccountManager.Admin.Panel.Pages.Inbox;

public class IndexModel : PageModel
{
    private readonly IRawMessageRepository _rawMessageRepository;
    private readonly IProcessingService _processingService;
    private readonly IAdminNotificationRepository _notificationRepository;


    public List<RawMessage> Messages { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 10;
    public int TotalPendingMessages { get; set; }

    public IndexModel(
        IRawMessageRepository rawMessageRepository,
        IProcessingService processingService,
        ILogger<IndexModel> logger, IAdminNotificationRepository notificationRepository)
    {
        _rawMessageRepository = rawMessageRepository;
        _processingService = processingService;
        _notificationRepository = notificationRepository;
       
    }

    public async Task OnGetAsync()
    {
        var pendingMessages = await _rawMessageRepository.GetPendingMessagesWithChannelAsync();
        var pendingMessagesQuery = pendingMessages.AsQueryable();

        TotalPendingMessages = pendingMessagesQuery.Count();
        TotalPages = (int)System.Math.Ceiling(TotalPendingMessages / (double)PageSize);

        Messages = pendingMessagesQuery
            .OrderByDescending(m => m.ReceivedAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    public async Task<IActionResult> OnPostIgnoreAsync(int id)
    {
        var message = await _rawMessageRepository.GetByIdAsync(id);
        if (message != null)
        {
            message.Status = RawMessageStatus.Ignored;
            _rawMessageRepository.Update(message);
            await _rawMessageRepository.SaveChangesAsync();
            StatusMessage = $"Message ID {id} has been ignored.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetProcessPreviewAsync(int id)
    {
        if (_processingService == null)
        {
            return new JsonResult(new { success = false, message = "Critical DI error: ProcessingService is not available." });
        }

        var parsedData = await _processingService.GetParsedDataPreviewAsync(id);

        if (parsedData == null)
        {
            return new JsonResult(new { success = false, message = "Could not parse message. Check logs for details or verify channel profile configuration." });
        }

        return new JsonResult(new { success = true, data = parsedData });
    }

    public async Task<IActionResult> OnPostProcessAndSaveAsync([FromBody] ProcessMessageViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new { success = false, message = "Invalid data submitted." });
        }

        var result = await _processingService.ProcessAndSaveAccountAsync(viewModel);

        if (result.Success)
        {
            
            if (result.IsNewAccount)
            {
                StatusMessage = $"Message ID {viewModel.RawMessageId} processed and new account '{result.AccountTitle}' created successfully!";
            }
            else
            {
                if (result.DetectedChanges.Any())
                {
                    var changesSummary = string.Join(", ", result.DetectedChanges.Select(c => c.FieldName));
                    var message = $"Account '{result.AccountTitle}' was updated: {changesSummary}";

                    // Generate URL using UrlHelper
                    var linkUrl = Url.Page("/Accounts/Details", new { id = result.AccountId });

                    var notification = new AdminNotification
                    {
                        Message = message,
                        LinkUrl = linkUrl
                    };
                    await _notificationRepository.AddAsync(notification);
                    await _notificationRepository.SaveChangesAsync();

                    StatusMessage = $"Message ID {viewModel.RawMessageId} processed. {message}";
                }
                else
                {
                    StatusMessage = $"Message ID {viewModel.RawMessageId} processed. No changes detected for account '{result.AccountTitle}'.";
                }
            }
            return new JsonResult(new { success = true });
        }

        return new JsonResult(new { success = false, message = "An error occurred while saving the account." });
    }
}
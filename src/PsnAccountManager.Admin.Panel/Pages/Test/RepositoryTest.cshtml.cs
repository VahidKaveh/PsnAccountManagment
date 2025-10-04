using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Admin.Panel.Pages.Test
{
    public class RepositoryTestModel : PageModel
    {
        private readonly IRawMessageRepository _rawMessageRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IChangeDetectionService _changeDetectionService;
        private readonly ILogger<RepositoryTestModel> _logger;

        [TempData]
        public string? StatusMessage { get; set; }

        public int PendingChangesCount { get; set; }
        public int PendingMessagesCount { get; set; }
        public int TotalActiveAccounts { get; set; }

        public RepositoryTestModel(
            IRawMessageRepository rawMessageRepo,
            IAccountRepository accountRepo,
            IChangeDetectionService changeDetectionService,
            ILogger<RepositoryTestModel> logger)
        {
            _rawMessageRepo = rawMessageRepo;
            _accountRepo = accountRepo;
            _changeDetectionService = changeDetectionService;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            try
            {
                // Test repository methods
                PendingChangesCount = await _rawMessageRepo.CountByStatusAsync(RawMessageStatus.PendingChange);
                PendingMessagesCount = await _rawMessageRepo.CountByStatusAsync(RawMessageStatus.Pending);
                TotalActiveAccounts = await _accountRepo.GetTotalCountAsync();

                _logger.LogInformation("Repository test completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during repository test");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnPostTestHashAsync()
        {
            try
            {
                var testMessage1 = "Test message for hash generation";
                var testMessage2 = "Test message for hash generation"; // Same content
                var testMessage3 = "Different message for hash generation"; // Different content

                var hash1 = _changeDetectionService.GenerateContentHash(testMessage1);
                var hash2 = _changeDetectionService.GenerateContentHash(testMessage2);
                var hash3 = _changeDetectionService.GenerateContentHash(testMessage3);

                var results = new
                {
                    SameContentHashesMatch = hash1 == hash2,
                    DifferentContentHashesDiffer = hash1 != hash3,
                    Hash1 = hash1[..8] + "...", // Show first 8 characters only
                    Hash3 = hash3[..8] + "...",
                    AllTestsPassed = (hash1 == hash2) && (hash1 != hash3)
                };

                StatusMessage = $"Hash Test - Same Content Match: {results.SameContentHashesMatch}, Different Content Differ: {results.DifferentContentHashesDiffer}, All Tests Passed: {results.AllTestsPassed}";

                _logger.LogInformation("Hash test results: Same={Same}, Different={Different}",
                    results.SameContentHashesMatch, results.DifferentContentHashesDiffer);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing hash generation");
                StatusMessage = $"Hash Test Error: {ex.Message}";
                return RedirectToPage();
            }
        }


        public async Task<IActionResult> OnPostTestPendingChangesAsync()
        {
            try
            {
                var pendingChanges = await _rawMessageRepo.GetPendingChangesAsync();
                var count = pendingChanges.Count();

                StatusMessage = $"Found {count} pending changes in database";
                _logger.LogInformation("Pending changes test: found {Count} items", count);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing pending changes");
                StatusMessage = $"Pending Changes Test Error: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostTestAccountRemovalAsync(int channelId = 1)
        {
            try
            {
                // Test with empty external IDs to see what would be marked for removal
                var emptyExternalIds = new List<string>();
                var activeAccounts = await _accountRepo.GetActiveAccountsForChannelAsync(channelId);
                var activeCount = activeAccounts.Count();

                StatusMessage = $"Channel {channelId} has {activeCount} active accounts. Would mark all for removal with empty external IDs list.";
                _logger.LogInformation("Account removal test: Channel {ChannelId} has {Count} active accounts",
                    channelId, activeCount);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing account removal");
                StatusMessage = $"Account Removal Test Error: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}
